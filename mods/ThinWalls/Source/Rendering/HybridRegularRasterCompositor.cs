using System;
using System.Collections.Generic;
using System.Linq;
using ThinWalls.Geometry;
using UnityEngine;

namespace ThinWalls.Rendering;

public sealed class HybridRegularCompositionException : InvalidOperationException
{
    public HybridRegularCompositionException(string message) : base(message)
    {
    }
}

public readonly struct HybridWallContactKey : IEquatable<HybridWallContactKey>
{
    public HybridWallContactKey(HybridWallQuadrant quadrant, HybridWallRayMask ray)
    {
        Quadrant = quadrant;
        Ray = ray;
    }

    public HybridWallQuadrant Quadrant { get; }
    public HybridWallRayMask Ray { get; }
    public bool IsValid => Quadrant != HybridWallQuadrant.None && Ray != HybridWallRayMask.None;

    public bool Equals(HybridWallContactKey other) => Quadrant == other.Quadrant && Ray == other.Ray;
    public override bool Equals(object? obj) => obj is HybridWallContactKey other && Equals(other);
    public override int GetHashCode() => ((int)Quadrant * 397) ^ (int)Ray;
    public static bool operator ==(HybridWallContactKey left, HybridWallContactKey right) => left.Equals(right);
    public static bool operator !=(HybridWallContactKey left, HybridWallContactKey right) => !left.Equals(right);
    public override string ToString() => $"{(int)Quadrant}:{(int)Ray}";
}

public readonly struct HybridWallContactVisual : IEquatable<HybridWallContactVisual>
{
    public HybridWallContactVisual(
        HybridWallContactKey contact,
        int materialId,
        ThinWallMaterialFamily family,
        ThinWallDamageGrade damage,
        bool door,
        ThinWallSide? ownerSide = null)
    {
        Contact = contact;
        MaterialId = materialId;
        Family = family;
        Damage = damage;
        Door = door;
        OwnerSide = ownerSide;
    }

    public HybridWallContactKey Contact { get; }
    public int MaterialId { get; }
    public ThinWallMaterialFamily Family { get; }
    public ThinWallDamageGrade Damage { get; }
    public bool Door { get; }
    public ThinWallSide? OwnerSide { get; }

    public bool Equals(HybridWallContactVisual other) =>
        Contact == other.Contact && MaterialId == other.MaterialId && Family == other.Family &&
        Damage == other.Damage && Door == other.Door && OwnerSide == other.OwnerSide;
    public override bool Equals(object? obj) => obj is HybridWallContactVisual other && Equals(other);
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = Contact.GetHashCode();
            hash = (hash * 397) ^ MaterialId;
            hash = (hash * 397) ^ (int)Family;
            hash = (hash * 397) ^ (int)Damage;
            hash = (hash * 397) ^ Door.GetHashCode();
            return (hash * 397) ^ (OwnerSide.HasValue ? (int)OwnerSide.Value : -1);
        }
    }
    public override string ToString() =>
        $"{Contact}:{MaterialId}:{(int)Family}:{(int)Damage}:{(Door ? 1 : 0)}:{(OwnerSide.HasValue ? (int)OwnerSide.Value : -1)}";
}

public static class HybridWallContactVisualSelector
{
    public static HybridWallContactVisual Select(
        HybridRegularCompositePlan plan,
        int index,
        IReadOnlyList<HybridWallContactVisual> visuals)
    {
        HybridWallContactKey contact = plan.ContactAt(index);
        HybridWallContactVisual[] matches = visuals.Where(visual => visual.Contact == contact).ToArray();
        if (matches.Length == 0)
        {
            throw new InvalidOperationException($"No Thin visual descriptor exists for hybrid contact {contact}.");
        }
        if (matches.Length == 1)
        {
            return matches[0];
        }

        HybridWallRasterPixel sample = plan.ThinAt(index);
        ThinWallSide side = contact.Ray is HybridWallRayMask.East or HybridWallRayMask.West
            ? HorizontalOwner(sample)
            : VerticalOwner(sample);
        HybridWallContactVisual[] owned = matches.Where(visual => visual.OwnerSide == side).ToArray();
        if (owned.Length != 1)
        {
            throw new InvalidOperationException(
                $"Hybrid contact {contact} has {owned.Length} descriptors for doubled owner side {side}; expected exactly one.");
        }
        return owned[0];
    }

    private static ThinWallSide HorizontalOwner(HybridWallRasterPixel sample) =>
        sample.Surface == HybridWallRasterSurface.Front || sample.SourceY < 42
            ? ThinWallSide.North
            : ThinWallSide.South;

    private static ThinWallSide VerticalOwner(HybridWallRasterPixel sample) =>
        sample.Surface == HybridWallRasterSurface.WestSide || sample.SourceX < 30
            ? ThinWallSide.East
            : ThinWallSide.West;
}

public sealed class CoreLinkedSemanticPlan
{
    internal CoreLinkedSemanticPlan(
        int linkIndex,
        HybridWallRasterPixel[] pixels,
        HybridWallRayMask[] causalRays)
    {
        LinkIndex = linkIndex;
        Pixels = pixels;
        CausalRays = causalRays;
    }

    public int LinkIndex { get; }

    public IReadOnlyList<HybridWallRasterPixel> Pixels { get; }

    private IReadOnlyList<HybridWallRayMask> CausalRays { get; }

    public HybridWallRasterPixel this[int x, int y]
    {
        get
        {
            if (x < 0 || x >= HybridWallRasterPlan.Size || y < 0 || y >= HybridWallRasterPlan.Size)
            {
                throw new ArgumentOutOfRangeException();
            }
            return Pixels[y * HybridWallRasterPlan.Size + x];
        }
    }

    public HybridWallRayMask CausalAt(int x, int y)
    {
        if (x < 0 || x >= HybridWallRasterPlan.Size || y < 0 || y >= HybridWallRasterPlan.Size)
        {
            throw new ArgumentOutOfRangeException();
        }
        return CausalRays[y * HybridWallRasterPlan.Size + x];
    }
}

/// <summary>
/// Analytic semantic ownership for one native Core linked-wall tile. Texture alpha is deliberately
/// absent from this compiler: installed bytes validate samples later, but cannot decide whether an
/// opaque pixel is ordinary body, an endpoint face, or a linked arm.
/// </summary>
public static class CoreLinkedSemanticCompiler
{
    private const int Size = HybridWallRasterPlan.Size;
    private const int TopMinX = 14;
    private const int TopMaxX = 46;
    private const int TopMinY = 25;
    private const int TopMaxY = 57;

    public static CoreLinkedSemanticPlan Compile(int linkIndex)
    {
        if (linkIndex < 0 || linkIndex > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(linkIndex));
        }

        var top = new bool[Size, Size];
        var topOwners = new HybridWallRayMask[Size, Size];
        FillTop(TopMinX, TopMinY, TopMaxX, TopMaxY, HybridWallRayMask.None);
        if ((linkIndex & (int)HybridWallRayMask.North) != 0)
        {
            FillTop(TopMinX, TopMaxY + 1, TopMaxX, Size - 1, HybridWallRayMask.North);
        }
        if ((linkIndex & (int)HybridWallRayMask.East) != 0)
        {
            FillTop(TopMaxX + 1, TopMinY, Size - 1, TopMaxY, HybridWallRayMask.East);
        }
        if ((linkIndex & (int)HybridWallRayMask.South) != 0)
        {
            FillTop(TopMinX, 0, TopMaxX, TopMinY - 1, HybridWallRayMask.South);
        }
        if ((linkIndex & (int)HybridWallRayMask.West) != 0)
        {
            FillTop(0, TopMinY, TopMinX - 1, TopMaxY, HybridWallRayMask.West);
        }

        var pixels = new HybridWallRasterPixel[Size * Size];
        var causalRays = new HybridWallRayMask[Size * Size];
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            if (!top[x, y])
            {
                continue;
            }

            HybridWallRayMask causalOwner = topOwners[x, y];
            Paint(x, y, HybridWallRasterSurface.Top, causalOwner, causalOwner);
            if (y == 0 || !top[x, y - 1])
            {
                for (int depth = 1; depth <= HybridWallProjectionProfile.FrontFacePixels; depth++)
                {
                    Paint(
                        x,
                        y - depth,
                        HybridWallRasterSurface.Front,
                        HybridWallRayMask.South | causalOwner,
                        causalOwner);
                }
            }
            if (x == 0 || !top[x - 1, y])
            {
                for (int depth = 1; depth <= HybridWallProjectionProfile.WestSidePixels; depth++)
                {
                    Paint(
                        x - depth,
                        y,
                        HybridWallRasterSurface.WestSide,
                        HybridWallRayMask.West | causalOwner,
                        causalOwner);
                }
            }
            if (x == Size - 1 || !top[x + 1, y])
            {
                for (int depth = 1; depth <= HybridWallProjectionProfile.EastSidePixels; depth++)
                {
                    Paint(
                        x + depth,
                        y,
                        HybridWallRasterSurface.EastSide,
                        HybridWallRayMask.East | causalOwner,
                        causalOwner);
                }
            }
        }

        bool[] structural = pixels.Select(pixel => pixel.IsStructural).ToArray();
        for (int ring = 1; ring <= HybridWallProjectionProfile.OutlinePixels + 1; ring++)
        {
            HybridWallRasterSurface surface = ring <= HybridWallProjectionProfile.OutlinePixels
                ? HybridWallRasterSurface.Outline
                : HybridWallRasterSurface.OutlineAntialias;
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                int index = y * Size + x;
                if (structural[index] || pixels[index].Surface != HybridWallRasterSurface.Transparent ||
                    !TryNearestStructural(structural, pixels, x, y, ring, out HybridWallRayMask owner) ||
                    HasStructuralWithin(structural, x, y, ring - 1))
                {
                    continue;
                }
                pixels[index] = new HybridWallRasterPixel(surface, 0, 0, (byte)linkIndex, owner);
            }
        }

        return new CoreLinkedSemanticPlan(linkIndex, pixels, causalRays);

        void FillTop(int minX, int minY, int maxX, int maxY, HybridWallRayMask owner)
        {
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                top[x, y] = true;
                topOwners[x, y] |= owner;
            }
        }

        void Paint(
            int x,
            int y,
            HybridWallRasterSurface surface,
            HybridWallRayMask owner,
            HybridWallRayMask cause)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size)
            {
                return;
            }
            int index = y * Size + x;
            if (surface < pixels[index].Surface)
            {
                return;
            }
            if (surface == pixels[index].Surface)
            {
                owner |= pixels[index].OwnerRays;
                cause |= causalRays[index];
            }
            pixels[index] = new HybridWallRasterPixel(
                surface,
                (byte)x,
                (byte)y,
                (byte)linkIndex,
                owner);
            causalRays[index] = cause;
        }
    }

    private static bool TryNearestStructural(
        bool[] structural,
        HybridWallRasterPixel[] pixels,
        int x,
        int y,
        int distance,
        out HybridWallRayMask owner)
    {
        owner = HybridWallRayMask.None;
        bool found = false;
        for (int offsetY = -distance; offsetY <= distance; offsetY++)
        for (int offsetX = -distance; offsetX <= distance; offsetX++)
        {
            int candidateX = x + offsetX;
            int candidateY = y + offsetY;
            if (candidateX < 0 || candidateX >= HybridWallRasterPlan.Size ||
                candidateY < 0 || candidateY >= HybridWallRasterPlan.Size)
            {
                continue;
            }
            int index = candidateY * HybridWallRasterPlan.Size + candidateX;
            if (!structural[index])
            {
                continue;
            }
            found = true;
            owner |= pixels[index].OwnerRays;
        }
        return found;
    }

    private static bool HasStructuralWithin(bool[] structural, int x, int y, int distance)
    {
        if (distance < 0)
        {
            return false;
        }
        for (int offsetY = -distance; offsetY <= distance; offsetY++)
        for (int offsetX = -distance; offsetX <= distance; offsetX++)
        {
            int candidateX = x + offsetX;
            int candidateY = y + offsetY;
            if (candidateX >= 0 && candidateX < HybridWallRasterPlan.Size &&
                candidateY >= 0 && candidateY < HybridWallRasterPlan.Size &&
                structural[candidateY * HybridWallRasterPlan.Size + candidateX])
            {
                return true;
            }
        }
        return false;
    }
}

/// <summary>
/// Maps analytically added ordinary-wall arm surfaces to measured straight Core donors.
/// Non-straight atlas slots classify topology only; their diagonal bevel pixels never
/// enter a square Thin-to-regular shoulder.
/// </summary>
public static class HybridRegularSquareDonor
{
    private const int TopMinX = 14;
    private const int TopMaxX = 46;
    private const int TopMinY = 25;
    private const int TopMaxY = 57;
    public static bool TryMap(
        HybridWallRasterPixel sample,
        HybridWallRayMask causalRays,
        out int donorLinkIndex,
        out int donorX,
        out int donorY)
    {
        donorLinkIndex = 0;
        donorX = sample.SourceX;
        donorY = sample.SourceY;
        if (!sample.IsStructural || causalRays == HybridWallRayMask.None)
        {
            return false;
        }

        if (sample.Surface == HybridWallRasterSurface.Top)
        {
            HybridWallRayMask horizontal = causalRays &
                                           (HybridWallRayMask.East | HybridWallRayMask.West);
            HybridWallRayMask vertical = causalRays &
                                         (HybridWallRayMask.North | HybridWallRayMask.South);
            if (horizontal != HybridWallRayMask.None &&
                (vertical == HybridWallRayMask.None || donorX < TopMinX || donorX > TopMaxX))
            {
                donorLinkIndex = (int)(HybridWallRayMask.East | HybridWallRayMask.West);
                donorX = HybridWallProjectionProfile.HorizontalWorldPhase(donorX);
                return true;
            }
            if (vertical.HasFlag(HybridWallRayMask.South) &&
                (donorY < TopMinY || !vertical.HasFlag(HybridWallRayMask.North)))
            {
                donorLinkIndex = (int)(HybridWallRayMask.North | HybridWallRayMask.South);
                donorY = HybridWallProjectionProfile.VerticalWorldPhase(HybridWallRayMask.South, donorY);
                return true;
            }
            if (vertical.HasFlag(HybridWallRayMask.North))
            {
                donorLinkIndex = (int)(HybridWallRayMask.North | HybridWallRayMask.South);
                donorY = HybridWallProjectionProfile.VerticalWorldPhase(HybridWallRayMask.North, donorY);
                return true;
            }
            return false;
        }

        if (sample.Surface == HybridWallRasterSurface.Front)
        {
            donorLinkIndex = (int)(HybridWallRayMask.East | HybridWallRayMask.West);
            donorX = HybridWallProjectionProfile.HorizontalWorldPhase(donorX);
            return true;
        }

        if (sample.Surface is HybridWallRasterSurface.WestSide or HybridWallRasterSurface.EastSide)
        {
            donorLinkIndex = (int)(HybridWallRayMask.North | HybridWallRayMask.South);
            if (causalRays.HasFlag(HybridWallRayMask.South) &&
                (donorY < TopMinY || !causalRays.HasFlag(HybridWallRayMask.North)))
            {
                donorY = HybridWallProjectionProfile.VerticalWorldPhase(HybridWallRayMask.South, donorY);
            }
            else if (causalRays.HasFlag(HybridWallRayMask.North))
            {
                donorY = HybridWallProjectionProfile.VerticalWorldPhase(HybridWallRayMask.North, donorY);
            }
            else
            {
                donorY = HybridWallProjectionProfile.VerticalWorldPhase(HybridWallRayMask.None, donorY);
            }
            return true;
        }

        return false;
    }
}

public sealed class HybridRegularCompositePlan
{
    internal HybridRegularCompositePlan(
        HybridWallRasterPixel[] thinSamples,
        HybridWallContactKey[] thinContacts,
        HybridWallRasterPixel[] originalSemantic,
        HybridWallRasterPixel[] transitionSemantic,
        byte[] outlineRings,
        bool[] aperture,
        bool[] originalOpaque,
        bool[] transitionOpaque,
        bool[] remappedRegularStructural,
        bool[] sideTContactContinuations,
        bool[] sideTContourApertures,
        bool[] shadowTopContinuations,
        HybridWallRayMask outgoingRays,
        int originalLinkIndex,
        int transitionLinkIndex,
        IReadOnlyList<HybridWallCornerRaster> corners)
    {
        ThinSamples = thinSamples;
        ThinContacts = thinContacts;
        OriginalSemantic = originalSemantic;
        TransitionSemantic = transitionSemantic;
        OutlineRings = outlineRings;
        Aperture = aperture;
        OriginalOpaque = originalOpaque;
        TransitionOpaque = transitionOpaque;
        RemappedRegularStructural = remappedRegularStructural;
        SideTContactContinuations = sideTContactContinuations;
        SideTContourApertures = sideTContourApertures;
        ShadowTopContinuations = shadowTopContinuations;
        OutgoingRays = outgoingRays;
        OriginalLinkIndex = originalLinkIndex;
        TransitionLinkIndex = transitionLinkIndex;
        Corners = corners.ToArray();
    }

    internal HybridWallRasterPixel[] ThinSamples { get; }
    internal HybridWallContactKey[] ThinContacts { get; }
    internal HybridWallRasterPixel[] OriginalSemantic { get; }
    internal HybridWallRasterPixel[] TransitionSemantic { get; }
    internal byte[] OutlineRings { get; }
    internal bool[] OriginalOpaque { get; }
    internal bool[] TransitionOpaque { get; }
    internal bool[] RemappedRegularStructural { get; }
    internal bool[] SideTContactContinuations { get; }
    internal bool[] SideTContourApertures { get; }
    internal bool[] ShadowTopContinuations { get; }
    internal HybridWallRayMask OutgoingRays { get; }
    internal IReadOnlyList<HybridWallCornerRaster> Corners { get; }

    public bool[] Aperture { get; }
    public int OriginalLinkIndex { get; }
    public int TransitionLinkIndex { get; }

    public HybridWallRasterPixel ThinAt(int x, int y) =>
        ThinSamples[y * HybridRegularRasterCompositor.CanvasSize + x];

    public HybridWallRasterPixel ThinAt(int index) => ThinSamples[index];

    public HybridWallContactKey ContactAt(int index) => ThinContacts[index];

    public HybridWallContactKey ContactAt(int x, int y) =>
        ThinContacts[y * HybridRegularRasterCompositor.CanvasSize + x];

    public HybridWallRasterPixel OriginalAt(int x, int y) =>
        OriginalSemantic[y * HybridRegularRasterCompositor.CanvasSize + x];

    public HybridWallRasterPixel TransitionAt(int x, int y) =>
        TransitionSemantic[y * HybridRegularRasterCompositor.CanvasSize + x];

    public bool HasSideTContactContinuationAt(int x, int y) =>
        SideTContactContinuations[y * HybridRegularRasterCompositor.CanvasSize + x];

    internal bool HasTopContinuationAtCanvas(int x, int y)
    {
        if (x < 0 || x >= HybridRegularRasterCompositor.CanvasSize ||
            y < 0 || y >= HybridRegularRasterCompositor.CanvasSize)
        {
            return false;
        }
        int index = y * HybridRegularRasterCompositor.CanvasSize + x;
        return ThinSamples[index].Surface == HybridWallRasterSurface.Top ||
               ShadowTopContinuations[index];
    }
}

public readonly struct HybridRegularSamplingLayers
{
    public HybridRegularSamplingLayers(
        Color32[] native,
        Color32[] exterior,
        IReadOnlyList<HybridRegularExteriorSamplingLayer> exteriorRegions,
        bool hasExteriorPixels)
    {
        Native = native;
        Exterior = exterior;
        ExteriorRegions = exteriorRegions;
        HasExteriorPixels = hasExteriorPixels;
    }

    public Color32[] Native { get; }

    public Color32[] Exterior { get; }

    public IReadOnlyList<HybridRegularExteriorSamplingLayer> ExteriorRegions { get; }

    public bool HasExteriorPixels { get; }
}

public readonly struct HybridRegularExteriorSamplingLayer
{
    public HybridRegularExteriorSamplingLayer(
        HybridRegularExteriorRegion region,
        Color32[] pixels,
        int width,
        int height,
        bool hasPixels)
    {
        Region = region;
        Pixels = pixels;
        Width = width;
        Height = height;
        HasPixels = hasPixels;
    }

    public HybridRegularExteriorRegion Region { get; }
    public Color32[] Pixels { get; }
    public int Width { get; }
    public int Height { get; }
    public bool HasPixels { get; }
}

public static class HybridRegularSamplingLayerCompiler
{
    public static HybridRegularSamplingLayers Compile(IReadOnlyList<Color32> composite)
    {
        int size = HybridRegularRasterCompositor.CanvasSize;
        int padding = HybridRegularRasterCompositor.Padding;
        int nativeSize = HybridWallRasterPlan.Size;
        if (composite == null || composite.Count != size * size)
        {
            throw new ArgumentException($"Hybrid regular raster must contain exactly {size * size} pixels.", nameof(composite));
        }

        var native = new Color32[nativeSize * nativeSize];
        var exterior = new Color32[composite.Count];
        bool hasExteriorPixels = false;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int index = y * size + x;
            bool inside = x >= padding && x < padding + nativeSize &&
                          y >= padding && y < padding + nativeSize;
            if (inside)
            {
                native[(y - padding) * nativeSize + x - padding] = composite[index];
            }
            else
            {
                exterior[index] = composite[index];
                hasExteriorPixels |= composite[index].a != 0;
            }
        }

        // The native layer is a tightly cropped clamped 60x60 texture. Every mip level
        // therefore terminates in Core pixels instead of averaging a one-pixel guard
        // back into thirty pixels of transparent padding.

        // Each submitted exterior quad receives its own tightly cropped, clamped
        // texture. Its innermost texel is therefore the actual gutter boundary at
        // every mip level; transparent pixels from the unsubmitted native hole can
        // never bleed into the visible contact.
        IReadOnlyList<HybridRegularExteriorRegion> exteriorGeometry =
            HybridRegularExteriorRegionCompiler.Compile();
        var exteriorRegions = new HybridRegularExteriorSamplingLayer[exteriorGeometry.Count];
        for (int regionIndex = 0; regionIndex < exteriorGeometry.Count; regionIndex++)
        {
            HybridRegularExteriorRegion region = exteriorGeometry[regionIndex];
            int minX = Mathf.RoundToInt(region.UvMinX * size);
            int maxX = Mathf.RoundToInt(region.UvMaxX * size);
            int minY = Mathf.RoundToInt(region.UvMinY * size);
            int maxY = Mathf.RoundToInt(region.UvMaxY * size);
            int width = maxX - minX;
            int height = maxY - minY;
            var pixels = new Color32[width * height];
            bool hasPixels = false;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Color32 pixel = exterior[(minY + y) * size + minX + x];
                pixels[y * width + x] = pixel;
                hasPixels |= pixel.a != 0;
            }
            exteriorRegions[regionIndex] = new HybridRegularExteriorSamplingLayer(
                region,
                pixels,
                width,
                height,
                hasPixels);
        }

        return new HybridRegularSamplingLayers(
            native,
            exterior,
            exteriorRegions,
            hasExteriorPixels);
    }
}

public readonly struct HybridRegularExteriorRegion
{
    public HybridRegularExteriorRegion(
        float centerX,
        float centerZ,
        float sizeX,
        float sizeZ,
        float uvMinX,
        float uvMinY,
        float uvMaxX,
        float uvMaxY,
        float altitudeOffset,
        float topVerticesAltitudeBias)
    {
        CenterX = centerX;
        CenterZ = centerZ;
        SizeX = sizeX;
        SizeZ = sizeZ;
        UvMinX = uvMinX;
        UvMinY = uvMinY;
        UvMaxX = uvMaxX;
        UvMaxY = uvMaxY;
        AltitudeOffset = altitudeOffset;
        TopVerticesAltitudeBias = topVerticesAltitudeBias;
    }

    public float CenterX { get; }
    public float CenterZ { get; }
    public float SizeX { get; }
    public float SizeZ { get; }
    public float UvMinX { get; }
    public float UvMinY { get; }
    public float UvMaxX { get; }
    public float UvMaxY { get; }
    public float AltitudeOffset { get; }
    public float TopVerticesAltitudeBias { get; }
    public float MinX => CenterX - SizeX * 0.5f;
    public float MaxX => CenterX + SizeX * 0.5f;
    public float MinZ => CenterZ - SizeZ * 0.5f;
    public float MaxZ => CenterZ + SizeZ * 0.5f;
}

public static class HybridRegularExteriorRegionCompiler
{
    private const float NativePlaneTopBias = 0.01f;

    public static IReadOnlyList<HybridRegularExteriorRegion> Compile() => new[]
    {
        new HybridRegularExteriorRegion(
            0f, -0.75f, 2f, 0.5f,
            0f, 0f, 1f, 0.25f,
            0f, 0f),
        new HybridRegularExteriorRegion(
            0f, 0.75f, 2f, 0.5f,
            0f, 0.75f, 1f, 1f,
            NativePlaneTopBias, 0f),
        new HybridRegularExteriorRegion(
            -0.75f, 0f, 0.5f, 1f,
            0f, 0.25f, 0.25f, 0.75f,
            0f, NativePlaneTopBias),
        new HybridRegularExteriorRegion(
            0.75f, 0f, 0.5f, 1f,
            0.75f, 0.25f, 1f, 0.75f,
            0f, NativePlaneTopBias),
    };
}

public static class HybridRegularRasterCompositor
{
    public const int CanvasSize = HybridWallRasterPlan.Size * 2;
    public const int Padding = HybridWallRasterPlan.Size / 2;
    private const int NativeSize = HybridWallRasterPlan.Size;

    public static HybridRegularCompositePlan Compile(
        IReadOnlyList<byte> nativeAlpha,
        IReadOnlyList<byte> transitionAlpha,
        IReadOnlyList<HybridWallCornerRaster> corners,
        int originalLinkIndex = 0)
    {
        ValidateNativeSamples(nativeAlpha, nameof(nativeAlpha));
        ValidateNativeSamples(transitionAlpha, nameof(transitionAlpha));
        if (originalLinkIndex < 0 || originalLinkIndex > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(originalLinkIndex));
        }

        int transitionLinkIndex = TransitionLinkIndex(originalLinkIndex, corners);
        CoreLinkedSemanticPlan originalPlan = CoreLinkedSemanticCompiler.Compile(originalLinkIndex);
        CoreLinkedSemanticPlan transitionPlan = CoreLinkedSemanticCompiler.Compile(transitionLinkIndex);
        int length = CanvasSize * CanvasSize;
        var thinSamples = new HybridWallRasterPixel[length];
        var thinContacts = new HybridWallContactKey[length];
        var originalSemantic = new HybridWallRasterPixel[length];
        var transitionSemantic = new HybridWallRasterPixel[length];
        var thinStructural = new bool[length];
        var outlineOwnedStructural = new bool[length];
        var unionStructural = new bool[length];
        var outlineRings = new byte[length];
        var aperture = new bool[length];
        var originalOpaque = new bool[length];
        var transitionOpaque = new bool[length];
        var remappedRegularStructural = new bool[length];
        var sideTContactContinuations = new bool[length];
        var sideTContourApertures = new bool[length];
        var shadowTopContinuations = new bool[length];
        HybridWallRayMask outgoing = HybridWallRayMask.None;
        HybridWallRayMask doorOutgoing = HybridWallRayMask.None;
        foreach (HybridWallCornerRaster corner in corners)
        {
            HybridWallRayMask eligible = HybridWallRegularApertureRecipe.EligibleRays(
                corner.Quadrant,
                corner.ActualThinRays);
            outgoing |= eligible;
            doorOutgoing |= eligible & corner.DoorRays;
        }
        HybridWallRayMask solidOutgoing = outgoing & ~doorOutgoing;
        HybridWallRayMask sideTOutgoing = HybridWallRayMask.None;
        foreach (HybridWallCornerRaster corner in corners)
        {
            sideTOutgoing |= corner.SideTRays;
        }
        sideTOutgoing &= solidOutgoing;
        HybridWallRayMask completeOutgoing = solidOutgoing & ~sideTOutgoing;

        for (int y = 0; y < NativeSize; y++)
        for (int x = 0; x < NativeSize; x++)
        {
            int nativeIndex = y * NativeSize + x;
            int canvasIndex = (y + Padding) * CanvasSize + x + Padding;
            originalOpaque[canvasIndex] = nativeAlpha[nativeIndex] != 0;
            transitionOpaque[canvasIndex] = transitionAlpha[nativeIndex] != 0;
            originalSemantic[canvasIndex] = originalPlan[x, y];
            transitionSemantic[canvasIndex] = transitionPlan[x, y];
            if (originalOpaque[canvasIndex])
            {
                unionStructural[canvasIndex] = originalSemantic[canvasIndex].IsStructural;
            }
        }

        MarkOriginalLinkedShadowContinuations(
            originalLinkIndex,
            originalPlan,
            shadowTopContinuations);

        // A solid Thin contact turns the regular wall into the corresponding ordinary
        // Core linked state inside the native tile. Only the exterior gutter remains
        // Thin width, producing a square shoulder at the tile boundary.
        for (int y = 0; y < NativeSize; y++)
        for (int x = 0; x < NativeSize; x++)
        {
            int index = (y + Padding) * CanvasSize + x + Padding;
            HybridWallRasterPixel transition = transitionSemantic[index];
            HybridWallRayMask ownedOutgoing = transitionPlan.CausalAt(x, y) & completeOutgoing;
            if (!transition.IsStructural || ownedOutgoing == HybridWallRayMask.None)
            {
                continue;
            }

            HybridWallRayMask ray = FirstRay(ownedOutgoing);
            thinSamples[index] = transition;
            thinContacts[index] = ContactForRay(corners, ray);
            thinStructural[index] = true;
            outlineOwnedStructural[index] = true;
            unionStructural[index] = true;
        }

        foreach (HybridWallCornerRaster corner in corners)
        {
            HybridWallRayMask sideTRays = corner.SideTRays & solidOutgoing;
            if (sideTRays == HybridWallRayMask.None)
            {
                continue;
            }

            // A side-T does not add the perpendicular ordinary-width arm from Core's T slot.
            // Each receiving tile participates only at its half of the seven-pixel perimeter
            // contact so the original contour can be opened beneath the one canonical gutter.
            foreach (HybridWallRasterWrite write in HybridWallRegularApertureRecipe.Compile(
                         corner.Quadrant,
                         sideTRays,
                         HybridWallRayMask.None,
                         HybridWallRayMask.None))
            {
                int canvasX = write.X + Padding;
                int canvasY = write.Y + Padding;
                if (canvasX < 0 || canvasX >= CanvasSize || canvasY < 0 || canvasY >= CanvasSize)
                {
                    continue;
                }
                int index = canvasY * CanvasSize + canvasX;
                sideTContactContinuations[index] = true;
                FillSideTContourContinuation(
                    sideTContourApertures,
                    originalSemantic,
                    originalOpaque,
                    thinSamples,
                    thinContacts,
                    thinStructural,
                    unionStructural,
                    canvasX,
                    canvasY,
                    write.Sample,
                    new HybridWallContactKey(corner.Quadrant, write.Sample.OwnerRays));
                if (write.Sample.Surface == HybridWallRasterSurface.Top)
                {
                    shadowTopContinuations[index] = true;
                }
            }
        }

        foreach (HybridWallCornerRaster corner in corners)
        {
            foreach (HybridWallRasterWrite write in HybridWallRegularApertureRecipe.Compile(
                         corner.Quadrant,
                         corner.GutterRays,
                         corner.DoubledRays & corner.GutterRays,
                         corner.DoorRays & corner.GutterRays))
            {
                int canvasX = write.X + Padding;
                int canvasY = write.Y + Padding;
                if (canvasX < 0 || canvasX >= CanvasSize || canvasY < 0 || canvasY >= CanvasSize)
                {
                    continue;
                }
                int index = canvasY * CanvasSize + canvasX;
                var contact = new HybridWallContactKey(corner.Quadrant, write.Sample.OwnerRays);
                if (!thinSamples[index].IsStructural || write.Sample.Surface > thinSamples[index].Surface ||
                    write.Sample.Surface == thinSamples[index].Surface && ContactRank(contact) < ContactRank(thinContacts[index]))
                {
                    thinSamples[index] = write.Sample;
                    thinContacts[index] = contact;
                }
                thinStructural[index] = true;
                outlineOwnedStructural[index] = true;
                unionStructural[index] = true;
            }
        }

        int contourDepth = HybridWallRasterCompiler.OutlineDepth + 1;
        for (int y = 0; y < CanvasSize; y++)
        for (int x = 0; x < CanvasSize; x++)
        {
            int index = y * CanvasSize + x;
            if (outlineOwnedStructural[index] || sideTContactContinuations[index] ||
                sideTContourApertures[index] ||
                HasStructuralWithin(outlineOwnedStructural, x, y, contourDepth) ||
                HasStructuralWithin(sideTContactContinuations, x, y, contourDepth))
            {
                aperture[index] = true;
            }
        }

        for (int y = 0; y < CanvasSize; y++)
        for (int x = 0; x < CanvasSize; x++)
        {
            int index = y * CanvasSize + x;
            if (!aperture[index] || unionStructural[index])
            {
                continue;
            }
            if (!outlineOwnedStructural[index] &&
                !HasStructuralWithin(outlineOwnedStructural, x, y, contourDepth))
            {
                // A non-owner side-T continuation opens the receiving wall contour,
                // but it never owns pixels or a second exterior outline. The canonical
                // gutter owner is the only plane allowed to derive that contour.
                continue;
            }
            outlineRings[index] = (byte)OutlineRing(unionStructural, x, y);
            thinContacts[index] = NearestThinContact(thinStructural, thinContacts, x, y, contourDepth);
        }

        return new HybridRegularCompositePlan(
            thinSamples,
            thinContacts,
            originalSemantic,
            transitionSemantic,
            outlineRings,
            aperture,
            originalOpaque,
            transitionOpaque,
            remappedRegularStructural,
            sideTContactContinuations,
            sideTContourApertures,
            shadowTopContinuations,
            outgoing,
            originalLinkIndex,
            transitionLinkIndex,
            corners);

        static int ContactRank(HybridWallContactKey contact) =>
            contact.IsValid ? ((int)contact.Quadrant << 4) | (int)contact.Ray : int.MaxValue;

        static HybridWallRayMask FirstRay(HybridWallRayMask rays)
        {
            foreach (HybridWallRayMask ray in new[]
                     {
                         HybridWallRayMask.North,
                         HybridWallRayMask.East,
                         HybridWallRayMask.South,
                         HybridWallRayMask.West,
                     })
            {
                if (rays.HasFlag(ray))
                {
                    return ray;
                }
            }
            return HybridWallRayMask.None;
        }

        static HybridWallContactKey ContactForRay(
            IReadOnlyList<HybridWallCornerRaster> sourceCorners,
            HybridWallRayMask ray)
        {
            foreach (HybridWallCornerRaster corner in sourceCorners)
            {
                if (HybridWallRegularApertureRecipe
                    .EligibleRays(corner.Quadrant, corner.ActualThinRays)
                    .HasFlag(ray))
                {
                    return new HybridWallContactKey(corner.Quadrant, ray);
                }
            }
            return default;
        }
    }

    private static void FillSideTContourContinuation(
        bool[] aperture,
        HybridWallRasterPixel[] originalSemantic,
        bool[] originalOpaque,
        HybridWallRasterPixel[] thinSamples,
        HybridWallContactKey[] thinContacts,
        bool[] thinStructural,
        bool[] unionStructural,
        int canvasX,
        int canvasY,
        HybridWallRasterPixel boundarySample,
        HybridWallContactKey contact)
    {
        HybridWallRayMask ray = contact.Ray;
        int nativeX = canvasX - Padding;
        int nativeY = canvasY - Padding;
        bool onReceivingBoundary = ray switch
        {
            HybridWallRayMask.South => nativeY == 0,
            HybridWallRayMask.North => nativeY == NativeSize - 1,
            HybridWallRayMask.West => nativeX == 0,
            HybridWallRayMask.East => nativeX == NativeSize - 1,
            _ => false,
        };
        if (!onReceivingBoundary)
        {
            return;
        }

        for (int depth = 0; depth <= HybridWallRasterCompiler.OutlineDepth; depth++)
        {
            int x = canvasX;
            int y = canvasY;
            switch (ray)
            {
                case HybridWallRayMask.South:
                    y += depth;
                    break;
                case HybridWallRayMask.North:
                    y -= depth;
                    break;
                case HybridWallRayMask.West:
                    x += depth;
                    break;
                case HybridWallRayMask.East:
                    x -= depth;
                    break;
            }
            if (x >= Padding && x < Padding + NativeSize &&
                y >= Padding && y < Padding + NativeSize)
            {
                int index = y * CanvasSize + x;
                aperture[index] = true;
                if (!originalOpaque[index] ||
                    originalSemantic[index].Surface is not (
                        HybridWallRasterSurface.Outline or
                        HybridWallRasterSurface.OutlineAntialias))
                {
                    continue;
                }

                HybridWallRasterPixel continuation = ShiftSideTContinuationSample(
                    boundarySample,
                    ray,
                    depth);
                if (!thinSamples[index].IsStructural ||
                    continuation.Surface > thinSamples[index].Surface ||
                    continuation.Surface == thinSamples[index].Surface &&
                    ContactRank(contact) < ContactRank(thinContacts[index]))
                {
                    thinSamples[index] = continuation;
                    thinContacts[index] = contact;
                }
                thinStructural[index] = true;
                unionStructural[index] = true;
            }
        }

        static int ContactRank(HybridWallContactKey value) =>
            value.IsValid ? ((int)value.Quadrant << 4) | (int)value.Ray : int.MaxValue;
    }

    private static HybridWallRasterPixel ShiftSideTContinuationSample(
        HybridWallRasterPixel sample,
        HybridWallRayMask ray,
        int depth)
    {
        int sourceX = sample.SourceX;
        int sourceY = sample.SourceY;
        switch (ray)
        {
            case HybridWallRayMask.South:
                sourceY += depth;
                break;
            case HybridWallRayMask.North:
                sourceY -= depth;
                break;
            case HybridWallRayMask.West:
                sourceX += depth;
                break;
            case HybridWallRayMask.East:
                sourceX -= depth;
                break;
        }

        return new HybridWallRasterPixel(
            sample.Surface,
            (byte)PositiveMod(sourceX, NativeSize),
            (byte)PositiveMod(sourceY, NativeSize),
            sample.SourceLinkIndex,
            sample.OwnerRays);

        static int PositiveMod(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }

    private static void MarkOriginalLinkedShadowContinuations(
        int originalLinkIndex,
        CoreLinkedSemanticPlan originalPlan,
        bool[] shadowTopContinuations)
    {
        HybridWallRayMask links = (HybridWallRayMask)originalLinkIndex;
        if (links.HasFlag(HybridWallRayMask.West))
        {
            for (int y = 0; y < NativeSize; y++)
            {
                if (originalPlan[0, y].Surface == HybridWallRasterSurface.Top)
                {
                    shadowTopContinuations[(y + Padding) * CanvasSize + Padding - 1] = true;
                }
            }
        }
        if (links.HasFlag(HybridWallRayMask.East))
        {
            for (int y = 0; y < NativeSize; y++)
            {
                if (originalPlan[NativeSize - 1, y].Surface == HybridWallRasterSurface.Top)
                {
                    shadowTopContinuations[(y + Padding) * CanvasSize + Padding + NativeSize] = true;
                }
            }
        }
        if (links.HasFlag(HybridWallRayMask.South))
        {
            for (int x = 0; x < NativeSize; x++)
            {
                if (originalPlan[x, 0].Surface == HybridWallRasterSurface.Top)
                {
                    shadowTopContinuations[(Padding - 1) * CanvasSize + x + Padding] = true;
                }
            }
        }
        if (links.HasFlag(HybridWallRayMask.North))
        {
            for (int x = 0; x < NativeSize; x++)
            {
                if (originalPlan[x, NativeSize - 1].Surface == HybridWallRasterSurface.Top)
                {
                    shadowTopContinuations[(Padding + NativeSize) * CanvasSize + x + Padding] = true;
                }
            }
        }
    }

    public static Color32[] Compose(
        IReadOnlyList<Color32> nativeTile,
        IReadOnlyList<Color32> transitionTile,
        HybridRegularCompositePlan plan,
        Func<HybridWallRasterPixel, Color32> structuralSample,
        Func<int, int, Color32> outlineSample)
        => ComposeOwned(
            nativeTile,
            transitionTile,
            plan,
            (_, sample) => structuralSample(sample),
            (x, y, _) => outlineSample(x, y),
            sample => transitionTile[sample.SourceY * NativeSize + sample.SourceX]);

    public static Color32[] ComposeOwned(
        IReadOnlyList<Color32> nativeTile,
        IReadOnlyList<Color32> transitionTile,
        HybridRegularCompositePlan plan,
        Func<int, HybridWallRasterPixel, Color32> structuralSample,
        Func<int, int, int, Color32> outlineSample,
        Func<HybridWallRasterPixel, Color32>? regularStructuralSample = null)
    {
        ValidateNativeSamples(nativeTile, nameof(nativeTile));
        ValidateNativeSamples(transitionTile, nameof(transitionTile));
        var output = new Color32[CanvasSize * CanvasSize];
        for (int y = 0; y < NativeSize; y++)
        for (int x = 0; x < NativeSize; x++)
        {
            output[(y + Padding) * CanvasSize + x + Padding] = nativeTile[y * NativeSize + x];
        }

        for (int index = 0; index < output.Length; index++)
        {
            if (!plan.Aperture[index])
            {
                continue;
            }
            HybridWallRasterPixel thin = plan.ThinSamples[index];
            HybridWallRasterPixel original = plan.OriginalSemantic[index];
            HybridWallRasterPixel transition = plan.TransitionSemantic[index];
            if (thin.IsStructural)
            {
                if (IsInsideNativeRegion(index))
                {
                    int nativeX = index % CanvasSize - Padding;
                    int nativeY = index / CanvasSize - Padding;
                    Color32 donor;
                    if (regularStructuralSample != null &&
                        TryStraightDonor(plan, index, thin, out HybridWallRasterPixel straightDonor))
                    {
                        donor = regularStructuralSample(straightDonor);
                    }
                    else
                    {
                        throw new HybridRegularCompositionException(
                            $"No straight Core donor exists for hybrid regular-wall pixel {nativeX},{nativeY} " +
                            $"({plan.ContactAt(index)}, {thin.Surface}, link {thin.SourceLinkIndex}).");
                    }
                    if (donor.a == 0)
                    {
                        throw new HybridRegularCompositionException(
                            $"Straight Core donor is transparent for hybrid regular-wall pixel {nativeX},{nativeY} " +
                            $"({plan.ContactAt(index)}, {thin.Surface}).");
                    }
                    output[index] = donor;
                }
                else
                {
                    output[index] = structuralSample(index, thin);
                }
                continue;
            }

            byte ring = plan.OutlineRings[index];
            if (ring == 0)
            {
                if (plan.SideTContourApertures[index] &&
                    plan.OriginalOpaque[index] &&
                    original.Surface is HybridWallRasterSurface.Outline or
                        HybridWallRasterSurface.OutlineAntialias)
                {
                    output[index] = default;
                }
                continue;
            }
            if (IsInsideNativeRegion(index) &&
                plan.OriginalOpaque[index] &&
                !plan.SideTContourApertures[index] &&
                original.Surface is HybridWallRasterSurface.Outline or
                    HybridWallRasterSurface.OutlineAntialias)
            {
                // The receiving straight wall keeps every contour byte outside the
                // mechanically declared Thin contact. Neighbourhood-based outline
                // derivation may see the new continuation, but it must not darken or
                // otherwise reclassify an unrelated native antialias/outline pixel.
                continue;
            }
            HybridWallRasterSurface expected = ring <= HybridWallRasterCompiler.OutlineDepth
                ? HybridWallRasterSurface.Outline
                : HybridWallRasterSurface.OutlineAntialias;
            if (original.Surface == expected && plan.OriginalOpaque[index])
            {
                continue;
            }
            int x = index % CanvasSize;
            int y = index / CanvasSize;
            output[index] = outlineSample(x, y, index);
            if (expected == HybridWallRasterSurface.OutlineAntialias)
            {
                output[index].a = (byte)Math.Min((int)output[index].a, 128);
            }
        }
        return output;
    }

    private static bool TryStraightDonor(
        HybridRegularCompositePlan plan,
        int index,
        HybridWallRasterPixel sample,
        out HybridWallRasterPixel donor)
    {
        HybridWallRayMask ray = plan.ContactAt(index).Ray;
        bool horizontal = ray is HybridWallRayMask.East or HybridWallRayMask.West;
        bool straight = horizontal
            ? sample.SourceLinkIndex == (byte)(HybridWallRayMask.East | HybridWallRayMask.West) &&
              sample.Surface is HybridWallRasterSurface.Top or HybridWallRasterSurface.Front
            : sample.SourceLinkIndex == (byte)(HybridWallRayMask.North | HybridWallRayMask.South) &&
              sample.Surface is HybridWallRasterSurface.Top or HybridWallRasterSurface.WestSide or
                  HybridWallRasterSurface.EastSide;
        if (straight)
        {
            donor = sample;
            return true;
        }
        if (HybridRegularSquareDonor.TryMap(
                sample,
                ray,
                out int donorLinkIndex,
                out int donorX,
                out int donorY))
        {
            donor = new HybridWallRasterPixel(
                sample.Surface,
                (byte)donorX,
                (byte)donorY,
                (byte)donorLinkIndex,
                ray);
            return true;
        }

        donor = default;
        return false;
    }

    public static bool IsInsideNativeRegion(int canvasIndex)
    {
        if (canvasIndex < 0 || canvasIndex >= CanvasSize * CanvasSize)
        {
            throw new ArgumentOutOfRangeException(nameof(canvasIndex));
        }
        int x = canvasIndex % CanvasSize;
        int y = canvasIndex / CanvasSize;
        return x >= Padding && x < Padding + NativeSize &&
               y >= Padding && y < Padding + NativeSize;
    }

    private static HybridWallContactKey NearestThinContact(
        bool[] thinStructural,
        HybridWallContactKey[] contacts,
        int x,
        int y,
        int maxDistance)
    {
        for (int distance = 1; distance <= maxDistance; distance++)
        {
            HybridWallContactKey selected = default;
            int selectedRank = int.MaxValue;
            for (int offsetY = -distance; offsetY <= distance; offsetY++)
            for (int offsetX = -distance; offsetX <= distance; offsetX++)
            {
                if (Math.Max(Math.Abs(offsetX), Math.Abs(offsetY)) != distance)
                {
                    continue;
                }
                int candidateX = x + offsetX;
                int candidateY = y + offsetY;
                if (candidateX < 0 || candidateX >= CanvasSize || candidateY < 0 || candidateY >= CanvasSize)
                {
                    continue;
                }
                int index = candidateY * CanvasSize + candidateX;
                if (!thinStructural[index] || !contacts[index].IsValid)
                {
                    continue;
                }
                int rank = ((int)contacts[index].Quadrant << 4) | (int)contacts[index].Ray;
                if (rank < selectedRank)
                {
                    selected = contacts[index];
                    selectedRank = rank;
                }
            }
            if (selected.IsValid)
            {
                return selected;
            }
        }
        return default;
    }

    public static int TransitionLinkIndex(
        int originalLinkIndex,
        IReadOnlyList<HybridWallCornerRaster> corners)
    {
        if (originalLinkIndex < 0 || originalLinkIndex > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(originalLinkIndex));
        }
        HybridWallRayMask outgoing = HybridWallRayMask.None;
        foreach (HybridWallCornerRaster corner in corners)
        {
            outgoing |= HybridWallRegularApertureRecipe.EligibleRays(corner.Quadrant, corner.ActualThinRays);
        }
        return originalLinkIndex | (int)outgoing;
    }

    private static int OutlineRing(bool[] structural, int x, int y)
    {
        for (int ring = 1; ring <= HybridWallRasterCompiler.OutlineDepth + 1; ring++)
        {
            if (HasStructuralWithin(structural, x, y, ring))
            {
                return ring;
            }
        }
        return 0;
    }

    private static bool HasStructuralWithin(bool[] structural, int x, int y, int distance)
    {
        for (int offsetY = -distance; offsetY <= distance; offsetY++)
        for (int offsetX = -distance; offsetX <= distance; offsetX++)
        {
            int candidateX = x + offsetX;
            int candidateY = y + offsetY;
            if (candidateX >= 0 && candidateX < CanvasSize &&
                candidateY >= 0 && candidateY < CanvasSize &&
                structural[candidateY * CanvasSize + candidateX])
            {
                return true;
            }
        }
        return false;
    }

    private static void ValidateNativeSamples<T>(IReadOnlyList<T> samples, string parameterName)
    {
        if (samples.Count != NativeSize * NativeSize)
        {
            throw new ArgumentException("A hybrid regular tile requires exactly 60x60 native samples.", parameterName);
        }
    }
}

public static class HybridWallAtlasSupport
{
    public static bool IsSupported(string? textureName, int width, int height) =>
        width == 320 && height == 320 && textureName is
            "Wall_Atlas_Bricks" or "Wall_Atlas_Planks" or "Wall_Atlas_Smooth";
}

public static class HybridRegularContactOwnership
{
    public static HybridWallRayMask RaysParticipatingIn(
        HybridWallQuadrant quadrant,
        HybridWallQuadrant occupiedQuadrants,
        HybridWallRayMask actualThinRays)
    {
        if (!occupiedQuadrants.HasFlag(quadrant))
        {
            return HybridWallRayMask.None;
        }

        return HybridWallRegularApertureRecipe.EligibleRays(quadrant, actualThinRays);
    }

    public static HybridWallRayMask RaysOwnedBy(
        HybridWallQuadrant quadrant,
        HybridWallQuadrant occupiedQuadrants,
        HybridWallRayMask actualThinRays)
    {
        HybridWallRayMask owned = HybridWallRayMask.None;
        foreach (HybridWallRayMask ray in new[]
                 {
                     HybridWallRayMask.North,
                     HybridWallRayMask.East,
                     HybridWallRayMask.South,
                     HybridWallRayMask.West,
                 })
        {
            if (!actualThinRays.HasFlag(ray) || OwnerQuadrant(ray, occupiedQuadrants) != quadrant)
            {
                continue;
            }
            owned |= ray;
        }
        return owned;
    }

    public static HybridWallRayMask SideTRaysParticipatingIn(
        HybridWallQuadrant quadrant,
        HybridWallQuadrant occupiedQuadrants,
        HybridWallRayMask actualThinRays)
    {
        HybridWallRayMask participating = RaysParticipatingIn(
            quadrant,
            occupiedQuadrants,
            actualThinRays);
        HybridWallRayMask result = HybridWallRayMask.None;
        foreach (HybridWallRayMask ray in new[]
                 {
                     HybridWallRayMask.North,
                     HybridWallRayMask.East,
                     HybridWallRayMask.South,
                     HybridWallRayMask.West,
                 })
        {
            if (participating.HasFlag(ray) && HasBothSideTQuadrants(ray, occupiedQuadrants))
            {
                result |= ray;
            }
        }
        return result;
    }

    public static HybridWallRayMask AllRegularOwnedRays(
        HybridWallQuadrant occupiedQuadrants,
        HybridWallRayMask actualThinRays)
    {
        HybridWallRayMask result = HybridWallRayMask.None;
        foreach (HybridWallQuadrant quadrant in new[]
                 {
                     HybridWallQuadrant.NorthEast,
                     HybridWallQuadrant.NorthWest,
                     HybridWallQuadrant.SouthEast,
                     HybridWallQuadrant.SouthWest,
                 })
        {
            if (occupiedQuadrants.HasFlag(quadrant))
            {
                result |= RaysOwnedBy(quadrant, occupiedQuadrants, actualThinRays);
            }
        }
        return result;
    }

    private static HybridWallQuadrant OwnerQuadrant(
        HybridWallRayMask ray,
        HybridWallQuadrant occupiedQuadrants)
    {
        HybridWallQuadrant[] preferences = ray switch
        {
            HybridWallRayMask.West => new[] { HybridWallQuadrant.NorthEast, HybridWallQuadrant.SouthEast },
            HybridWallRayMask.East => new[] { HybridWallQuadrant.NorthWest, HybridWallQuadrant.SouthWest },
            HybridWallRayMask.South => new[] { HybridWallQuadrant.NorthEast, HybridWallQuadrant.NorthWest },
            HybridWallRayMask.North => new[] { HybridWallQuadrant.SouthEast, HybridWallQuadrant.SouthWest },
            _ => Array.Empty<HybridWallQuadrant>(),
        };
        foreach (HybridWallQuadrant candidate in preferences)
        {
            if (occupiedQuadrants.HasFlag(candidate))
            {
                return candidate;
            }
        }
        return HybridWallQuadrant.None;
    }

    private static bool HasBothSideTQuadrants(
        HybridWallRayMask ray,
        HybridWallQuadrant occupiedQuadrants)
    {
        HybridWallQuadrant required = ray switch
        {
            HybridWallRayMask.South => HybridWallQuadrant.NorthWest | HybridWallQuadrant.NorthEast,
            HybridWallRayMask.North => HybridWallQuadrant.SouthWest | HybridWallQuadrant.SouthEast,
            HybridWallRayMask.West => HybridWallQuadrant.NorthEast | HybridWallQuadrant.SouthEast,
            HybridWallRayMask.East => HybridWallQuadrant.NorthWest | HybridWallQuadrant.SouthWest,
            _ => HybridWallQuadrant.None,
        };
        return required != HybridWallQuadrant.None &&
               (occupiedQuadrants & required) == required;
    }
}
