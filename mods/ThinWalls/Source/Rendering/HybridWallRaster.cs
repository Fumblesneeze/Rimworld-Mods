using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ThinWalls.Rendering;

public enum HybridWallRasterSurface : byte
{
    Transparent = 0,
    OutlineAntialias = 1,
    Outline = 2,
    WestSide = 3,
    EastSide = 4,
    Front = 5,
    Top = 6,
}

public readonly struct HybridWallRasterPixel : IEquatable<HybridWallRasterPixel>
{
    public HybridWallRasterPixel(
        HybridWallRasterSurface surface,
        byte sourceX,
        byte sourceY,
        byte sourceLinkIndex = 0,
        HybridWallRayMask ownerRays = HybridWallRayMask.None)
    {
        Surface = surface;
        SourceX = sourceX;
        SourceY = sourceY;
        SourceLinkIndex = sourceLinkIndex;
        OwnerRays = ownerRays;
    }

    public HybridWallRasterSurface Surface { get; }

    public byte SourceX { get; }

    public byte SourceY { get; }

    public byte SourceLinkIndex { get; }

    public HybridWallRayMask OwnerRays { get; }

    public bool IsStructural => Surface >= HybridWallRasterSurface.WestSide;

    public bool Equals(HybridWallRasterPixel other) =>
        Surface == other.Surface && SourceX == other.SourceX && SourceY == other.SourceY &&
        SourceLinkIndex == other.SourceLinkIndex && OwnerRays == other.OwnerRays;

    public override bool Equals(object? obj) => obj is HybridWallRasterPixel other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (int)Surface;
            hash = (hash * 397) ^ SourceX;
            hash = (hash * 397) ^ SourceY;
            hash = (hash * 397) ^ SourceLinkIndex;
            return (hash * 397) ^ (int)OwnerRays;
        }
    }
}

public sealed class HybridWallRasterPlan
{
    internal HybridWallRasterPlan(
        HybridWallRayMask rays,
        HybridWallRayMask doubledRays,
        HybridWallRayMask doorRays,
        HybridWallQuadrant clearedQuadrants,
        HybridWallRasterPixel[] pixels)
    {
        Rays = rays;
        DoubledRays = doubledRays;
        DoorRays = doorRays;
        ClearedQuadrants = clearedQuadrants;
        Pixels = pixels;
    }

    public const int Size = 60;

    public HybridWallRayMask Rays { get; }

    public HybridWallRayMask DoubledRays { get; }

    public HybridWallRayMask DoorRays { get; }

    public HybridWallQuadrant ClearedQuadrants { get; }

    public int TopWidth => DoubledRays == Rays && Rays != HybridWallRayMask.None
        ? HybridWallRasterCompiler.DoubledTopWidth
        : HybridWallRasterCompiler.StandardTopWidth;

    public IReadOnlyList<HybridWallRasterPixel> Pixels { get; }

    public HybridWallRasterPixel this[int x, int y]
    {
        get
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size)
            {
                throw new ArgumentOutOfRangeException();
            }

            return Pixels[y * Size + x];
        }
    }

    public int Count(HybridWallRasterSurface surface) => Pixels.Count(pixel => pixel.Surface == surface);
}

public static class HybridWallRasterCompiler
{
    public const int Size = HybridWallRasterPlan.Size;
    public const int StandardTopWidth = 7;
    public const int DoubledTopWidth = 14;
    public const int FrontDepth = 22;
    public const int WestSideDepth = 11;
    public const int EastSideDepth = 10;
    public const int OutlineDepth = 2;
    public const int DoorFrameLength = 3;

    private const int Center = 30;

    public static HybridWallRasterPlan Compile(
        HybridWallRayMask rays,
        bool doubled = false,
        HybridWallQuadrant clearedQuadrants = HybridWallQuadrant.None,
        bool includeOutline = true,
        HybridWallRayMask doorRays = HybridWallRayMask.None) =>
        Compile(
            rays,
            doubled ? rays : HybridWallRayMask.None,
            clearedQuadrants,
            includeOutline,
            doorRays);

    public static HybridWallRasterPlan Compile(
        HybridWallRayMask rays,
        HybridWallRayMask doubledRays,
        HybridWallQuadrant clearedQuadrants = HybridWallQuadrant.None,
        bool includeOutline = true,
        HybridWallRayMask doorRays = HybridWallRayMask.None)
    {
        doorRays &= rays;
        var pixels = new HybridWallRasterPixel[Size * Size];
        bool[,] top = BuildTopMask(rays, doubledRays, doorRays, out HybridWallRayMask[,] owners);
        PaintFaces(top, owners, rays, doorRays, pixels);
        PaintSouthFacingVerticalEndpoint(rays, doubledRays, doorRays, pixels);
        PaintTop(top, owners, rays, doubledRays, pixels);
        PaintHorizontalForeground(rays, doorRays, pixels);
        ClipNorthEndpointAgainstHorizontalArms(rays, doubledRays, doorRays, pixels);
        OccludeSouthBridgeBehindHorizontalFacade(rays, doubledRays, doorRays, pixels);

        if (includeOutline)
        {
            PaintOutline(pixels);
        }

        ClearQuadrants(pixels, clearedQuadrants);
        RemoveJoinOutline(pixels, clearedQuadrants);
        RemoveProjectedJoinCaps(pixels, rays, doubledRays, clearedQuadrants);
        return new HybridWallRasterPlan(rays, doubledRays, doorRays, clearedQuadrants, pixels);
    }

    private static bool[,] BuildTopMask(
        HybridWallRayMask rays,
        HybridWallRayMask doubledRays,
        HybridWallRayMask doorRays,
        out HybridWallRayMask[,] owners)
    {
        var top = new bool[Size, Size];
        owners = new HybridWallRayMask[Size, Size];

        if (rays.HasFlag(HybridWallRayMask.North))
        {
            (int verticalMinX, int verticalMaxX) = CenteredBand(Width(HybridWallRayMask.North));
            int maxY = doorRays.HasFlag(HybridWallRayMask.North)
                ? Center + DoorFrameLength - 1
                : Size - 1;
            Fill(top, owners, verticalMinX, Center, verticalMaxX, maxY, HybridWallRayMask.North);
        }
        if (rays.HasFlag(HybridWallRayMask.South))
        {
            (int verticalMinX, int verticalMaxX) = CenteredBand(Width(HybridWallRayMask.South));
            int minY = doorRays.HasFlag(HybridWallRayMask.South)
                ? Center - DoorFrameLength + 1
                : 0;
            Fill(top, owners, verticalMinX, minY, verticalMaxX, Center, HybridWallRayMask.South);
        }
        if (rays.HasFlag(HybridWallRayMask.East))
        {
            int width = Width(HybridWallRayMask.East);
            int horizontalMinY = Center + ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ;
            int horizontalMaxY = horizontalMinY + width - 1;
            int maxX = doorRays.HasFlag(HybridWallRayMask.East)
                ? Center + DoorFrameLength - 1
                : Size - 1;
            Fill(top, owners, Center, horizontalMinY, maxX, horizontalMaxY, HybridWallRayMask.East);
        }
        if (rays.HasFlag(HybridWallRayMask.West))
        {
            int width = Width(HybridWallRayMask.West);
            int horizontalMinY = Center + ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ;
            int horizontalMaxY = horizontalMinY + width - 1;
            int minX = doorRays.HasFlag(HybridWallRayMask.West)
                ? Center - DoorFrameLength + 1
                : 0;
            Fill(top, owners, minX, horizontalMinY, Center, horizontalMaxY, HybridWallRayMask.West);
        }

        HybridWallRayMask horizontalRays = rays &
                                           (HybridWallRayMask.East | HybridWallRayMask.West);
        HybridWallRayMask horizontalWallRays = horizontalRays & ~doorRays;
        if (rays.HasFlag(HybridWallRayMask.South) &&
            !doorRays.HasFlag(HybridWallRayMask.South) &&
            horizontalRays != HybridWallRayMask.None)
        {
            // Boundary balancing translates horizontal projection eight raster rows.
            // A solid horizontal wall needs the complete South bridge behind its
            // facade. A door-only contact has no such facade: connect only the
            // three fixed frame pixels and leave the moving-leaf aperture empty.
            if (horizontalWallRays != HybridWallRayMask.None)
            {
                (int verticalMinX, int verticalMaxX) = CenteredBand(Width(HybridWallRayMask.South));
                int horizontalWidth = Math.Max(
                    horizontalWallRays.HasFlag(HybridWallRayMask.East) ? Width(HybridWallRayMask.East) : 0,
                    horizontalWallRays.HasFlag(HybridWallRayMask.West) ? Width(HybridWallRayMask.West) : 0);
                int horizontalMaxY = Center + ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ +
                                     horizontalWidth - 1;
                Fill(
                    top,
                    owners,
                    verticalMinX,
                    Center + 1,
                    verticalMaxX,
                    horizontalMaxY,
                    HybridWallRayMask.South);
            }
            else
            {
                int bridgeMaxY = Center + ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ - 1;
                if (horizontalRays.HasFlag(HybridWallRayMask.East))
                {
                    Fill(
                        top,
                        owners,
                        Center,
                        Center + 1,
                        Center + DoorFrameLength - 1,
                        bridgeMaxY,
                        HybridWallRayMask.East);
                }
                if (horizontalRays.HasFlag(HybridWallRayMask.West))
                {
                    Fill(
                        top,
                        owners,
                        Center - DoorFrameLength + 1,
                        Center + 1,
                        Center,
                        bridgeMaxY,
                        HybridWallRayMask.West);
                }
            }
        }

        return top;

        int Width(HybridWallRayMask ray) => doubledRays.HasFlag(ray) ? DoubledTopWidth : StandardTopWidth;

        (int Min, int Max) CenteredBand(int width)
        {
            int min = Center - width / 2;
            return (min, min + width - 1);
        }
    }

    private static void PaintFaces(
        bool[,] top,
        HybridWallRayMask[,] owners,
        HybridWallRayMask topologyRays,
        HybridWallRayMask doorRays,
        HybridWallRasterPixel[] pixels)
    {
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (!top[x, y])
                {
                    continue;
                }

                HybridWallRayMask pixelOwners = owners[x, y];
                bool hasHorizontalOwner = (pixelOwners & (HybridWallRayMask.East | HybridWallRayMask.West)) != 0;
                bool hasVerticalOwner = (pixelOwners & (HybridWallRayMask.North | HybridWallRayMask.South)) != 0;
                bool connectedNorthEndpoint =
                    topologyRays.HasFlag(HybridWallRayMask.North) &&
                    (topologyRays & (HybridWallRayMask.East | HybridWallRayMask.West)) != HybridWallRayMask.None &&
                    pixelOwners.HasFlag(HybridWallRayMask.North);
                bool verticalDoorCap = !hasHorizontalOwner &&
                    (pixelOwners & doorRays & (HybridWallRayMask.North | HybridWallRayMask.South)) != 0;
                bool horizontalDoorCap = !hasVerticalOwner &&
                    (pixelOwners & doorRays & (HybridWallRayMask.East | HybridWallRayMask.West)) != 0;

                if (!verticalDoorCap && !connectedNorthEndpoint && (y == 0 || !top[x, y - 1]))
                {
                    for (int depth = 1; depth <= FrontDepth; depth++)
                    {
                        PaintStructural(
                            pixels,
                            x,
                            y - depth,
                            HybridWallRasterSurface.Front,
                            HorizontalRunningBondX(x, 25 - depth),
                            25 - depth,
                            SourceLink(topologyRays, owners[x, y], HybridWallRasterSurface.Front),
                            owners[x, y]);
                    }
                }

                if (hasVerticalOwner && !horizontalDoorCap && (x == 0 || !top[x - 1, y]))
                {
                    for (int depth = 1; depth <= WestSideDepth; depth++)
                    {
                        PaintStructural(
                            pixels,
                            x - depth,
                            y,
                            HybridWallRasterSurface.WestSide,
                            14 - depth,
                            y,
                            SourceLink(topologyRays, owners[x, y], HybridWallRasterSurface.WestSide),
                            owners[x, y]);
                    }
                }

                if (hasVerticalOwner && !horizontalDoorCap && (x == Size - 1 || !top[x + 1, y]))
                {
                    for (int depth = 1; depth <= EastSideDepth; depth++)
                    {
                        PaintStructural(
                            pixels,
                            x + depth,
                            y,
                            HybridWallRasterSurface.EastSide,
                            46 + depth,
                            y,
                            SourceLink(topologyRays, owners[x, y], HybridWallRasterSurface.EastSide),
                            owners[x, y]);
                    }
                }
            }
        }
    }

    private static void PaintTop(
        bool[,] top,
        HybridWallRayMask[,] owners,
        HybridWallRayMask topologyRays,
        HybridWallRayMask doubledRays,
        HybridWallRasterPixel[] pixels)
    {
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (!top[x, y])
                {
                    continue;
                }

                HybridWallRayMask owner = owners[x, y];
                int sourceLink = SourceLink(topologyRays, owner, HybridWallRasterSurface.Top);
                bool horizontal = (owner & (HybridWallRayMask.East | HybridWallRayMask.West)) != 0;
                int sourceX;
                int sourceY;
                if (horizontal)
                {
                    int width = WidthForOwner(
                        owner,
                        HybridWallRayMask.East | HybridWallRayMask.West,
                        doubledRays);
                    int minY = Center + ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ;
                    sourceX = HorizontalRunningBondX(x, y);
                    sourceY = ScaleBand(y - minY, width, 25, 57);
                }
                else
                {
                    int width = WidthForOwner(
                        owner,
                        HybridWallRayMask.North | HybridWallRayMask.South,
                        doubledRays);
                    int minX = Center - width / 2;
                    sourceX = ScaleBand(x - minX, width, 14, 46);
                    sourceY = y;
                }

                pixels[Index(x, y)] = new HybridWallRasterPixel(
                    HybridWallRasterSurface.Top,
                    (byte)sourceX,
                    (byte)sourceY,
                    (byte)sourceLink,
                    owner);
            }
        }

        static int WidthForOwner(
            HybridWallRayMask owner,
            HybridWallRayMask axis,
            HybridWallRayMask doubled) =>
            (owner & axis & doubled) != HybridWallRayMask.None
                ? DoubledTopWidth
                : StandardTopWidth;
    }

    private static void PaintSouthFacingVerticalEndpoint(
        HybridWallRayMask rays,
        HybridWallRayMask doubledRays,
        HybridWallRayMask doorRays,
        HybridWallRasterPixel[] pixels)
    {
        if (!rays.HasFlag(HybridWallRayMask.North) ||
            rays.HasFlag(HybridWallRayMask.South) ||
            doorRays.HasFlag(HybridWallRayMask.North) ||
            (rays & (HybridWallRayMask.East | HybridWallRayMask.West)) != HybridWallRayMask.None)
        {
            return;
        }

        int topWidth = doubledRays.HasFlag(HybridWallRayMask.North)
            ? DoubledTopWidth
            : StandardTopWidth;
        int topMin = Center - topWidth / 2;
        int topMax = topMin + topWidth - 1;
        int facadeMin = topMin - WestSideDepth;
        int facadeMax = topMax + EastSideDepth;
        int facadeWidth = facadeMax - facadeMin + 1;
        for (int x = facadeMin; x <= facadeMax; x++)
        for (int depth = 1; depth <= FrontDepth; depth++)
        {
            int sourceY = 25 - depth;
            int sourceX = ScaleBand(x - facadeMin, facadeWidth, 0, Size - 1);
            PaintStructural(
                pixels,
                x,
                Center - depth,
                HybridWallRasterSurface.Front,
                HorizontalRunningBondX(sourceX, sourceY),
                sourceY,
                10,
                HybridWallRayMask.North);
        }
    }

    private static void PaintOutline(HybridWallRasterPixel[] pixels)
    {
        bool[] structural = pixels.Select(pixel => pixel.IsStructural).ToArray();
        for (int ring = 1; ring <= OutlineDepth + 1; ring++)
        {
            HybridWallRasterSurface surface = ring <= OutlineDepth
                ? HybridWallRasterSurface.Outline
                : HybridWallRasterSurface.OutlineAntialias;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int index = Index(x, y);
                    if (structural[index] || pixels[index].Surface != HybridWallRasterSurface.Transparent)
                    {
                        continue;
                    }

                    if (!HasStructuralWithin(structural, x, y, ring) || HasStructuralWithin(structural, x, y, ring - 1))
                    {
                        continue;
                    }

                    (byte sourceX, byte sourceY) = OutlineSample(x, y);
                    pixels[index] = new HybridWallRasterPixel(surface, sourceX, sourceY);
                }
            }
        }
    }

    private static void PaintHorizontalForeground(
        HybridWallRayMask rays,
        HybridWallRayMask doorRays,
        HybridWallRasterPixel[] pixels)
    {
        HybridWallRayMask horizontal = rays & (HybridWallRayMask.East | HybridWallRayMask.West);
        if (horizontal == HybridWallRayMask.None)
        {
            return;
        }

        for (int x = 0; x < Size; x++)
        {
            HybridWallRayMask owner = x < Center
                ? HybridWallRayMask.West
                : HybridWallRayMask.East;
            if (!horizontal.HasFlag(owner) || doorRays.HasFlag(owner))
            {
                continue;
            }

            for (int y = 8 + ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ;
                 y <= 29 + ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ;
                 y++)
            {
                int sourceY = y - ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ - 5;
                pixels[Index(x, y)] = new HybridWallRasterPixel(
                    HybridWallRasterSurface.Front,
                    (byte)HorizontalRunningBondX(x, sourceY),
                    (byte)sourceY,
                    10,
                    owner);
            }
        }
    }

    private static void ClipNorthEndpointAgainstHorizontalArms(
        HybridWallRayMask rays,
        HybridWallRayMask doubledRays,
        HybridWallRayMask doorRays,
        HybridWallRasterPixel[] pixels)
    {
        if (!rays.HasFlag(HybridWallRayMask.North) ||
            rays.HasFlag(HybridWallRayMask.South) ||
            doorRays.HasFlag(HybridWallRayMask.North))
        {
            return;
        }

        HybridWallRayMask horizontal = rays &
                                       (HybridWallRayMask.East | HybridWallRayMask.West);
        if (horizontal == HybridWallRayMask.None)
        {
            return;
        }

        int northTopWidth = doubledRays.HasFlag(HybridWallRayMask.North)
            ? DoubledTopWidth
            : StandardTopWidth;
        int northTopMinX = Center - northTopWidth / 2;
        int northTopMaxX = northTopMinX + northTopWidth - 1;
        int facadeMinX = northTopMinX - WestSideDepth;
        int facadeMaxX = northTopMaxX + EastSideDepth;
        HybridWallRayMask horizontalWalls = horizontal & ~doorRays;

        // A continuing north arm starts above the horizontal facade. Remove the
        // complete north terminal projection first so neither its side caps nor
        // its lower top plane can survive on the non-incident half of an L/T.
        // Pixels already owned by a horizontal wall remain part of that facade.
        for (int y = 8; y <= 37; y++)
        for (int x = facadeMinX; x <= facadeMaxX; x++)
        {
            ClearNorthOwnedPixel(x, y);
        }

        foreach (HybridWallRayMask ray in new[] { HybridWallRayMask.West, HybridWallRayMask.East })
        {
            if (!horizontal.HasFlag(ray))
            {
                continue;
            }

            if (doorRays.HasFlag(ray))
            {
                int frameMinX = ray == HybridWallRayMask.West
                    ? Center - DoorFrameLength + 1
                    : Center;
                int frameMaxX = ray == HybridWallRayMask.West
                    ? Center
                    : Center + DoorFrameLength - 1;
                for (int y = 16; y <= 37; y++)
                for (int x = frameMinX; x <= frameMaxX; x++)
                {
                    if (HorizontalWallOwnerAt(x, horizontalWalls) != HybridWallRayMask.None)
                    {
                        continue;
                    }

                    int sourceY = y - ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ - 5;
                    pixels[Index(x, y)] = new HybridWallRasterPixel(
                        HybridWallRasterSurface.Front,
                        (byte)HorizontalRunningBondX(x, sourceY),
                        (byte)sourceY,
                        10,
                        ray);
                }
            }
        }

        for (int y = 16; y <= 37; y++)
        for (int x = facadeMinX; x <= facadeMaxX; x++)
        {
            HybridWallRayMask wallOwner = HorizontalWallOwnerAt(x, horizontalWalls);
            if (wallOwner == HybridWallRayMask.None)
            {
                continue;
            }

            int sourceY = y - ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ - 5;
            pixels[Index(x, y)] = new HybridWallRasterPixel(
                HybridWallRasterSurface.Front,
                (byte)HorizontalRunningBondX(x, sourceY),
                (byte)sourceY,
                10,
                wallOwner);
        }

        void ClearNorthOwnedPixel(int x, int y)
        {
            int index = Index(x, y);
            HybridWallRasterPixel pixel = pixels[index];
            if ((pixel.OwnerRays & HybridWallRayMask.North) != 0 &&
                (pixel.OwnerRays & horizontalWalls) == 0)
            {
                pixels[index] = default;
            }
        }

        static HybridWallRayMask HorizontalWallOwnerAt(int x, HybridWallRayMask walls)
        {
            if (x < Center)
            {
                return walls.HasFlag(HybridWallRayMask.West)
                    ? HybridWallRayMask.West
                    : HybridWallRayMask.None;
            }
            if (x > Center)
            {
                return walls.HasFlag(HybridWallRayMask.East)
                    ? HybridWallRayMask.East
                    : HybridWallRayMask.None;
            }

            return walls.HasFlag(HybridWallRayMask.East)
                ? HybridWallRayMask.East
                : walls.HasFlag(HybridWallRayMask.West)
                    ? HybridWallRayMask.West
                    : HybridWallRayMask.None;
        }
    }

    private static void OccludeSouthBridgeBehindHorizontalFacade(
        HybridWallRayMask rays,
        HybridWallRayMask doubledRays,
        HybridWallRayMask doorRays,
        HybridWallRasterPixel[] pixels)
    {
        if (!rays.HasFlag(HybridWallRayMask.South) ||
            doorRays.HasFlag(HybridWallRayMask.South))
        {
            return;
        }

        HybridWallRayMask horizontal = rays &
                                       (HybridWallRayMask.East | HybridWallRayMask.West) &
                                       ~doorRays;
        if (horizontal == HybridWallRayMask.None)
        {
            return;
        }

        int bridgeWidth = doubledRays.HasFlag(HybridWallRayMask.South)
            ? DoubledTopWidth
            : StandardTopWidth;
        int minX = Center - bridgeWidth / 2;
        int maxX = minX + bridgeWidth - 1;
        int minY = Center + 1;
        int maxY = 29 + ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ;
        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            HybridWallRayMask owner = x < Center && horizontal.HasFlag(HybridWallRayMask.West)
                ? HybridWallRayMask.West
                : x > Center && horizontal.HasFlag(HybridWallRayMask.East)
                    ? HybridWallRayMask.East
                    : horizontal.HasFlag(HybridWallRayMask.East)
                        ? HybridWallRayMask.East
                        : HybridWallRayMask.West;
            int sourceY = y - ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ - 5;
            pixels[Index(x, y)] = new HybridWallRasterPixel(
                HybridWallRasterSurface.Front,
                (byte)HorizontalRunningBondX(x, sourceY),
                (byte)sourceY,
                10,
                owner);
        }
    }

    private static int HorizontalRunningBondX(int x, int sourceY)
    {
        _ = sourceY;
        // A completed edge is printed from two vertex partitions: the East half of the
        // left vertex, followed by the West half of the right vertex. Rotate the Core
        // straight donor by half a tile so those world-space halves assemble source
        // columns 0..59 instead of resetting from 30..59 back to 0..29 at mid-edge.
        return PositiveMod(x + Center, Size);
    }

    private static void ClearQuadrants(HybridWallRasterPixel[] pixels, HybridWallQuadrant quadrants)
    {
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                HybridWallQuadrant quadrant = x >= Center
                    ? (y >= Center ? HybridWallQuadrant.NorthEast : HybridWallQuadrant.SouthEast)
                    : (y >= Center ? HybridWallQuadrant.NorthWest : HybridWallQuadrant.SouthWest);
                if (quadrants.HasFlag(quadrant))
                {
                    pixels[Index(x, y)] = default;
                }
            }
        }
    }

    private static void RemoveJoinOutline(HybridWallRasterPixel[] pixels, HybridWallQuadrant quadrants)
    {
        if (quadrants == HybridWallQuadrant.None)
        {
            return;
        }

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                HybridWallRasterSurface surface = pixels[Index(x, y)].Surface;
                if (surface != HybridWallRasterSurface.Outline &&
                    surface != HybridWallRasterSurface.OutlineAntialias)
                {
                    continue;
                }

                if (TouchesClearedQuadrant(x, y, quadrants))
                {
                    pixels[Index(x, y)] = default;
                }
            }
        }
    }

    private static bool TouchesClearedQuadrant(int x, int y, HybridWallQuadrant quadrants)
    {
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            int candidateX = x + offsetX;
            int candidateY = y + offsetY;
            if (candidateX < 0 || candidateX >= Size || candidateY < 0 || candidateY >= Size)
            {
                continue;
            }

            HybridWallQuadrant quadrant = candidateX >= Center
                ? (candidateY >= Center ? HybridWallQuadrant.NorthEast : HybridWallQuadrant.SouthEast)
                : (candidateY >= Center ? HybridWallQuadrant.NorthWest : HybridWallQuadrant.SouthWest);
            if (quadrants.HasFlag(quadrant))
            {
                return true;
            }
        }

        return false;
    }

    private static void RemoveProjectedJoinCaps(
        HybridWallRasterPixel[] pixels,
        HybridWallRayMask rays,
        HybridWallRayMask doubledRays,
        HybridWallQuadrant quadrants)
    {
        foreach (HybridWallQuadrant quadrant in new[]
                 {
                     HybridWallQuadrant.NorthEast,
                     HybridWallQuadrant.NorthWest,
                     HybridWallQuadrant.SouthEast,
                     HybridWallQuadrant.SouthWest,
                 })
        {
            if (!quadrants.HasFlag(quadrant))
            {
                continue;
            }
            HybridWallRayMask joins = HybridWallRegularApertureRecipe.EligibleRays(quadrant, rays);
            if (joins.HasFlag(HybridWallRayMask.West))
            {
                ClearHorizontalOutlineCap(31, 33, HybridWallRayMask.West);
            }
            if (joins.HasFlag(HybridWallRayMask.East))
            {
                ClearHorizontalOutlineCap(27, 29, HybridWallRayMask.East);
            }
            if (joins.HasFlag(HybridWallRayMask.South))
            {
                ClearOutlineRect(9, 31, 49, 33);
            }
            if (joins.HasFlag(HybridWallRayMask.North))
            {
                ClearOutlineRect(9, 27, 49, 29);
            }
        }

        void ClearHorizontalOutlineCap(int minX, int maxX, HybridWallRayMask ray)
        {
            int outlineRadius = OutlineDepth + 1;
            int topWidth = doubledRays.HasFlag(ray) ? DoubledTopWidth : StandardTopWidth;
            int minY = 8 + ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ - outlineRadius;
            int maxY = Center + ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ +
                       topWidth - 1 + outlineRadius;
            ClearOutlineRect(minX, minY, maxX, maxY);
        }

        void ClearOutlineRect(int minX, int minY, int maxX, int maxY)
        {
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                int index = Index(x, y);
                if (pixels[index].Surface is HybridWallRasterSurface.Outline or
                    HybridWallRasterSurface.OutlineAntialias)
                {
                    pixels[index] = default;
                }
            }
        }
    }

    private static void PaintStructural(
        HybridWallRasterPixel[] pixels,
        int x,
        int y,
        HybridWallRasterSurface surface,
        int sourceX,
        int sourceY,
        int sourceLinkIndex,
        HybridWallRayMask ownerRays)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size)
        {
            return;
        }

        int index = Index(x, y);
        if (pixels[index].Surface >= surface)
        {
            return;
        }

        pixels[index] = new HybridWallRasterPixel(
            surface,
            (byte)Math.Max(0, Math.Min(59, sourceX)),
            (byte)Math.Max(0, Math.Min(59, sourceY)),
            (byte)sourceLinkIndex,
            ownerRays);
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
            if (candidateX >= 0 && candidateX < Size && candidateY >= 0 && candidateY < Size &&
                structural[Index(candidateX, candidateY)])
            {
                return true;
            }
        }

        return false;
    }

    private static (byte X, byte Y) OutlineSample(int x, int y)
    {
        int distanceWest = x;
        int distanceEast = Size - 1 - x;
        int distanceSouth = y;
        int distanceNorth = Size - 1 - y;
        int minimum = Math.Min(Math.Min(distanceWest, distanceEast), Math.Min(distanceSouth, distanceNorth));
        if (minimum == distanceWest) return (1, (byte)y);
        if (minimum == distanceEast) return (58, (byte)y);
        if (minimum == distanceSouth) return ((byte)x, 1);
        return ((byte)x, 59);
    }

    private static void Fill(
        bool[,] target,
        HybridWallRayMask[,] owners,
        int minX,
        int minY,
        int maxX,
        int maxY,
        HybridWallRayMask owner)
    {
        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            target[x, y] = true;
            owners[x, y] |= owner;
        }
    }

    private static int SourceLink(
        HybridWallRayMask topologyRays,
        HybridWallRayMask ownerRays,
        HybridWallRasterSurface surface)
    {
        _ = topologyRays;
        if (surface == HybridWallRasterSurface.Front)
        {
            return 10;
        }
        if (surface is HybridWallRasterSurface.WestSide or HybridWallRasterSurface.EastSide)
        {
            return 5;
        }

        HybridWallRayMask horizontal = ownerRays & (HybridWallRayMask.East | HybridWallRayMask.West);
        HybridWallRayMask vertical = ownerRays & (HybridWallRayMask.North | HybridWallRayMask.South);
        if (horizontal != 0 && vertical == 0)
        {
            return 10;
        }
        if (vertical != 0 && horizontal == 0)
        {
            return 5;
        }

        return 10;
    }

    private static int ScaleBand(int offset, int width, int sourceMin, int sourceMax)
    {
        if (width <= 1)
        {
            return sourceMin;
        }

        float progress = Math.Max(0, Math.Min(width - 1, offset)) / (float)(width - 1);
        return (int)Math.Round(sourceMin + (sourceMax - sourceMin) * progress);
    }

    private static int PositiveMod(int value, int modulus)
    {
        int result = value % modulus;
        return result < 0 ? result + modulus : result;
    }

    private static int Index(int x, int y) => y * Size + x;
}

public readonly struct HybridWallRasterWrite
{
    public HybridWallRasterWrite(int x, int y, HybridWallRasterPixel sample)
    {
        X = x;
        Y = y;
        Sample = sample;
    }

    public int X { get; }
    public int Y { get; }
    public HybridWallRasterPixel Sample { get; }
}

public static class HybridWallRegularApertureRecipe
{
    public static IReadOnlyList<HybridWallRasterWrite> Compile(
        HybridWallQuadrant quadrant,
        HybridWallRayMask actualThinRays,
        HybridWallRayMask doubledRays,
        HybridWallRayMask doorRays)
    {
        HybridWallRayMask eligible = EligibleRays(quadrant, actualThinRays);
        if (eligible == HybridWallRayMask.None)
        {
            return Array.Empty<HybridWallRasterWrite>();
        }

        _ = doorRays;
        var writes = new Dictionary<(int X, int Y), HybridWallRasterPixel>();
        foreach (HybridWallRayMask ray in new[]
                 {
                     HybridWallRayMask.North,
                     HybridWallRayMask.East,
                     HybridWallRayMask.South,
                     HybridWallRayMask.West,
                 })
        {
            if (!eligible.HasFlag(ray))
            {
                continue;
            }
            if (ray is HybridWallRayMask.East or HybridWallRayMask.West)
            {
                PaintHorizontal(ray);
            }
            else
            {
                PaintVertical(ray);
            }
        }

        return writes
            .OrderBy(pair => pair.Key.Y)
            .ThenBy(pair => pair.Key.X)
            .Select(pair => new HybridWallRasterWrite(pair.Key.X, pair.Key.Y, pair.Value))
            .ToArray();

        void PaintHorizontal(HybridWallRayMask ray)
        {
            bool fromWest = ray == HybridWallRayMask.West;
            bool wallIsNorth = quadrant is HybridWallQuadrant.NorthEast or HybridWallQuadrant.NorthWest;
            int startBoundary = wallIsNorth ? 0 : 59;
            int width = doubledRays.HasFlag(ray)
                ? HybridWallRasterCompiler.DoubledTopWidth
                : HybridWallRasterCompiler.StandardTopWidth;

            // The padded half-arm reaches the corner at its real grid-edge cross-section.
            int outerLength = doorRays.HasFlag(ray)
                ? HybridWallRasterCompiler.DoorFrameLength - 1
                : 30;
            for (int step = 0; step <= outerLength; step++)
            {
                int x = fromWest ? -step : 59 + step;
                PaintHorizontalColumn(
                    x,
                    startBoundary,
                    width,
                    ray,
                    includeProjection: !doorRays.HasFlag(ray));
            }

            // The Thin participant stops at the native tile perimeter. The regular
            // participant's augmented Core link state owns the ordinary-width interior.
        }

        void PaintVertical(HybridWallRayMask ray)
        {
            bool fromSouth = ray == HybridWallRayMask.South;
            bool wallIsEast = quadrant is HybridWallQuadrant.NorthEast or HybridWallQuadrant.SouthEast;
            int startCenter = wallIsEast ? 0 : 59;
            int width = doubledRays.HasFlag(ray)
                ? HybridWallRasterCompiler.DoubledTopWidth
                : HybridWallRasterCompiler.StandardTopWidth;

            int outerLength = doorRays.HasFlag(ray)
                ? HybridWallRasterCompiler.DoorFrameLength - 1
                : 30;
            for (int step = 0; step <= outerLength; step++)
            {
                int y = fromSouth ? -step : 59 + step;
                PaintVerticalRow(
                    y,
                    startCenter,
                    width,
                    ray,
                    includeProjection: !doorRays.HasFlag(ray));
            }

            // No native-region interpolation is legal: the width change is one square
            // shoulder at the centered native tile boundary.
        }

        void PaintHorizontalColumn(
            int x,
            int boundary,
            int width,
            HybridWallRayMask ray,
            bool includeProjection)
        {
            for (int topOffset = 0; topOffset < width; topOffset++)
            {
                Add(x, boundary + topOffset, new HybridWallRasterPixel(
                    HybridWallRasterSurface.Top,
                    (byte)PositiveMod(x, HybridWallRasterPlan.Size),
                    (byte)ScaleBand(topOffset, width, 25, 57),
                    10,
                    ray));
            }
            if (!includeProjection)
            {
                return;
            }
            for (int depth = 1; depth <= HybridWallRasterCompiler.FrontDepth; depth++)
            {
                int sourceY = 25 - depth;
                Add(x, boundary - depth, new HybridWallRasterPixel(
                    HybridWallRasterSurface.Front,
                    (byte)HorizontalRunningBondX(x, sourceY),
                    (byte)sourceY,
                    10,
                    ray));
            }
        }

        void PaintVerticalRow(
            int y,
            int center,
            int width,
            HybridWallRayMask ray,
            bool includeProjection)
        {
            int half = width / 2;
            int minTop = center - half;
            int maxTop = minTop + width - 1;
            for (int x = minTop; x <= maxTop; x++)
            {
                Add(x, y, new HybridWallRasterPixel(
                    HybridWallRasterSurface.Top,
                    (byte)ScaleBand(x - minTop, width, 14, 46),
                    (byte)VerticalPhase(ray, y),
                    5,
                    ray));
            }
            if (!includeProjection)
            {
                return;
            }
            for (int depth = 1; depth <= HybridWallRasterCompiler.WestSideDepth; depth++)
            {
                Add(minTop - depth, y, new HybridWallRasterPixel(
                    HybridWallRasterSurface.WestSide,
                    (byte)(14 - depth),
                    (byte)VerticalPhase(ray, y),
                    5,
                    ray));
            }
            for (int depth = 1; depth <= HybridWallRasterCompiler.EastSideDepth; depth++)
            {
                Add(maxTop + depth, y, new HybridWallRasterPixel(
                    HybridWallRasterSurface.EastSide,
                    (byte)(46 + depth),
                    (byte)VerticalPhase(ray, y),
                    5,
                    ray));
            }
        }

        void Add(int x, int y, HybridWallRasterPixel sample)
        {
            if ((sample.OwnerRays & (HybridWallRayMask.East | HybridWallRayMask.West)) != 0)
            {
                y += ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ;
            }
            var key = (x, y);
            if (!writes.TryGetValue(key, out HybridWallRasterPixel existing) ||
                sample.Surface > existing.Surface)
            {
                writes[key] = sample;
            }
        }

        int ScaleBand(int offset, int width, int sourceMin, int sourceMax)
        {
            if (width <= 1)
            {
                return sourceMin;
            }
            float progress = Math.Max(0, Math.Min(width - 1, offset)) / (float)(width - 1);
            return (int)Math.Round(sourceMin + (sourceMax - sourceMin) * progress);
        }

        int HorizontalRunningBondX(int x, int sourceY)
        {
            _ = sourceY;
            return HybridWallProjectionProfile.HorizontalWorldPhase(x);
        }

        int VerticalPhase(HybridWallRayMask ray, int y) =>
            HybridWallProjectionProfile.VerticalWorldPhase(ray, y);

        int PositiveMod(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }

    public static HybridWallRayMask EligibleRays(
        HybridWallQuadrant quadrant,
        HybridWallRayMask actualThinRays) =>
        actualThinRays & (quadrant switch
        {
            HybridWallQuadrant.NorthEast => HybridWallRayMask.West | HybridWallRayMask.South,
            HybridWallQuadrant.NorthWest => HybridWallRayMask.East | HybridWallRayMask.South,
            HybridWallQuadrant.SouthEast => HybridWallRayMask.West | HybridWallRayMask.North,
            HybridWallQuadrant.SouthWest => HybridWallRayMask.East | HybridWallRayMask.North,
            _ => HybridWallRayMask.None,
        });
}

public static class HybridWallStoneFacadeDonor
{
    public static bool TryMap(
        HybridWallRasterPixel sample,
        out HybridWallRasterPixel donor)
    {
        HybridWallRayMask verticalOwners = sample.OwnerRays &
                                           (HybridWallRayMask.North | HybridWallRayMask.South);
        HybridWallRayMask horizontalOwners = sample.OwnerRays &
                                             (HybridWallRayMask.East | HybridWallRayMask.West);
        if (verticalOwners == HybridWallRayMask.None ||
            horizontalOwners != HybridWallRayMask.None)
        {
            donor = default;
            return false;
        }

        int depth;
        int span;
        switch (sample.Surface)
        {
            case HybridWallRasterSurface.WestSide:
                depth = 14 - sample.SourceX;
                span = HybridWallRasterCompiler.WestSideDepth;
                break;
            case HybridWallRasterSurface.EastSide:
                depth = sample.SourceX - 46;
                span = HybridWallRasterCompiler.EastSideDepth;
                break;
            default:
                donor = default;
                return false;
        }
        if (depth < 1 || depth > span)
        {
            donor = default;
            return false;
        }

        float progress = span <= 1 ? 0f : (depth - 1) / (float)(span - 1);
        int facadeY = (int)Math.Round(24f + (3f - 24f) * progress);
        donor = new HybridWallRasterPixel(
            sample.Surface,
            (byte)HybridWallProjectionProfile.HorizontalWorldPhase(sample.SourceY),
            (byte)facadeY,
            10,
            sample.OwnerRays);
        return true;
    }
}

public enum HybridWallTreatmentMark : byte
{
    None = 0,
    DetailDark = 1,
    DetailLight = 2,
    DamageDark = 3,
    DamageChip = 4,
    DamageLight = 5,
    DamageVoid = 6,
    DoorSeam = 7,
    DoorHardwareDark = 8,
    DoorHardwareLight = 9,
    JunctionSeam = 10,
}

public static class HybridWallTreatmentCoverage
{
    public static byte Alpha(byte sourceAlpha, HybridWallTreatmentMark mark) =>
        mark == HybridWallTreatmentMark.DamageVoid ? (byte)0 : sourceAlpha;
}

public static class HybridWallTreatmentColorizer
{
    public static Color32 Apply(Color32 color, HybridWallTreatmentMark mark)
    {
        return mark switch
        {
            HybridWallTreatmentMark.DetailDark => Scale(color, 0.68f),
            HybridWallTreatmentMark.DetailLight => Lerp(color, new Color32(255, 255, 255, color.a), 0.22f),
            HybridWallTreatmentMark.DamageDark => Scale(color, 0.25f),
            HybridWallTreatmentMark.DamageChip => Scale(color, 0.10f),
            HybridWallTreatmentMark.DamageLight => Lerp(color, new Color32(255, 255, 255, color.a), 0.65f),
            HybridWallTreatmentMark.DamageVoid => new Color32(0, 0, 0, 0),
            HybridWallTreatmentMark.DoorSeam => Scale(color, 0.14f),
            HybridWallTreatmentMark.DoorHardwareDark => Scale(color, 0.12f),
            HybridWallTreatmentMark.DoorHardwareLight =>
                Lerp(color, new Color32(255, 226, 142, color.a), 0.72f),
            HybridWallTreatmentMark.JunctionSeam => Scale(color, 0.32f),
            _ => color,
        };
    }

    private static Color32 Scale(Color32 color, float factor) => new(
        (byte)Math.Max(0, Math.Min(255, (int)Math.Round(color.r * factor))),
        (byte)Math.Max(0, Math.Min(255, (int)Math.Round(color.g * factor))),
        (byte)Math.Max(0, Math.Min(255, (int)Math.Round(color.b * factor))),
        color.a);

    private static Color32 Lerp(Color32 left, Color32 right, float amount) => new(
        Blend(left.r, right.r, amount),
        Blend(left.g, right.g, amount),
        Blend(left.b, right.b, amount),
        left.a);

    private static byte Blend(byte left, byte right, float amount) =>
        (byte)Math.Max(0, Math.Min(255, (int)Math.Round(left + (right - left) * amount)));
}

public static class HybridWallSurfaceTreatmentRecipe
{
    public static IReadOnlyList<HybridWallTreatmentMark> Compile(
        HybridWallRasterPlan plan,
        ThinWallMaterialFamily family,
        ThinWallDamageGrade damage,
        bool door)
    {
        var marks = new HybridWallTreatmentMark[plan.Pixels.Count];
        for (int y = 0; y < HybridWallRasterPlan.Size; y++)
        for (int x = 0; x < HybridWallRasterPlan.Size; x++)
        {
            HybridWallRasterPixel pixel = plan[x, y];
            if (!pixel.IsStructural)
            {
                continue;
            }

            int index = y * HybridWallRasterPlan.Size + x;
            if (IsJunctionSeam(plan, pixel, x, y))
            {
                marks[index] = HybridWallTreatmentMark.JunctionSeam;
                continue;
            }

            marks[index] = At(pixel, plan.Rays, family, damage, door, x, y);
        }

        return marks;
    }

    private static bool IsJunctionSeam(
        HybridWallRasterPlan plan,
        HybridWallRasterPixel pixel,
        int x,
        int y)
    {
        if (pixel.Surface != HybridWallRasterSurface.Top)
        {
            return false;
        }

        HybridWallRayMask wallRays = plan.Rays & ~plan.DoorRays;
        HybridWallRayMask verticalRays = wallRays &
                                          (HybridWallRayMask.North | HybridWallRayMask.South);
        HybridWallRayMask horizontalRays = wallRays &
                                            (HybridWallRayMask.East | HybridWallRayMask.West);
        if (verticalRays == HybridWallRayMask.None || horizontalRays == HybridWallRayMask.None)
        {
            return false;
        }

        foreach (HybridWallRayMask vertical in new[]
                 {
                     HybridWallRayMask.North,
                     HybridWallRayMask.South,
                 })
        foreach (HybridWallRayMask horizontal in new[]
                 {
                     HybridWallRayMask.East,
                     HybridWallRayMask.West,
                 })
        {
            if (!wallRays.HasFlag(vertical) || !wallRays.HasFlag(horizontal))
            {
                continue;
            }

            int verticalWidth = plan.DoubledRays.HasFlag(vertical)
                ? HybridWallRasterCompiler.DoubledTopWidth
                : HybridWallRasterCompiler.StandardTopWidth;
            int horizontalWidth = plan.DoubledRays.HasFlag(horizontal)
                ? HybridWallRasterCompiler.DoubledTopWidth
                : HybridWallRasterCompiler.StandardTopWidth;
            int steps = Math.Min((verticalWidth - 1) / 2, (horizontalWidth - 1) / 2);
            int centerX = 30;
            int centerY = 30 + ThinWallRenderGeometry.StructuralRasterOffsetPixelsZ +
                          (horizontalWidth / 2);
            int dx = horizontal == HybridWallRayMask.East ? 1 : -1;
            int dy = vertical == HybridWallRayMask.North ? 1 : -1;
            for (int step = 0; step <= steps; step++)
            {
                if (x == centerX + (dx * step) && y == centerY + (dy * step))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static HybridWallTreatmentMark At(
        HybridWallRasterPixel pixel,
        HybridWallRayMask rays,
        ThinWallMaterialFamily family,
        ThinWallDamageGrade damage,
        bool door,
        int x,
        int y)
    {
        if (!pixel.IsStructural)
        {
            return HybridWallTreatmentMark.None;
        }
        if (door)
        {
            HybridWallTreatmentMark hardware = DoorHardware(pixel.Surface, rays, x, y);
            if (hardware != HybridWallTreatmentMark.None)
            {
                return hardware;
            }
            if (IsDoorSeam(rays, x, y))
            {
                return HybridWallTreatmentMark.DoorSeam;
            }
        }

        HybridWallTreatmentMark damaged = Damage(family, damage, pixel, rays);
        return damaged != HybridWallTreatmentMark.None
            ? damaged
            : Detail(family, pixel.Surface, x, y);
    }

    private static bool IsDoorSeam(HybridWallRayMask rays, int x, int y)
    {
        bool horizontal = (rays & (HybridWallRayMask.East | HybridWallRayMask.West)) != 0 &&
                          (rays & (HybridWallRayMask.North | HybridWallRayMask.South)) == 0;
        return horizontal ? x is 29 or 30 : y is 29 or 30;
    }

    private static HybridWallTreatmentMark DoorHardware(
        HybridWallRasterSurface surface,
        HybridWallRayMask rays,
        int x,
        int y)
    {
        bool horizontal = (rays & (HybridWallRayMask.East | HybridWallRayMask.West)) != 0 &&
                          (rays & (HybridWallRayMask.North | HybridWallRayMask.South)) == 0;
        if (horizontal && surface == HybridWallRasterSurface.Front)
        {
            if (y == 18 && x is 26 or 27 or 32 or 33)
            {
                return HybridWallTreatmentMark.DoorHardwareDark;
            }
            if (y == 19 && x is 27 or 32)
            {
                return HybridWallTreatmentMark.DoorHardwareLight;
            }
        }
        if (!horizontal && surface is HybridWallRasterSurface.WestSide or HybridWallRasterSurface.EastSide)
        {
            if (x == 25 && y is 26 or 27 || x == 34 && y is 32 or 33)
            {
                return HybridWallTreatmentMark.DoorHardwareDark;
            }
            if (x == 24 && y == 27 || x == 35 && y == 32)
            {
                return HybridWallTreatmentMark.DoorHardwareLight;
            }
        }
        return HybridWallTreatmentMark.None;
    }

    private static HybridWallTreatmentMark Detail(
        ThinWallMaterialFamily family,
        HybridWallRasterSurface surface,
        int x,
        int y)
    {
        if (family == ThinWallMaterialFamily.Wood &&
            surface is HybridWallRasterSurface.Front or HybridWallRasterSurface.WestSide or HybridWallRasterSurface.EastSide)
        {
            int localX = PositiveMod(x, 30);
            int localY = PositiveMod(y, 18);
            int panelY = localY % 9;
            int staggeredSeamX = localY < 9 ? 20 : 10;
            if (localX == staggeredSeamX && panelY is >= 1 and <= 7 || localY == 9)
            {
                return HybridWallTreatmentMark.DetailDark;
            }
            if ((localY is 4 or 11) && localX is >= 5 and <= 8 ||
                (localY is 7 or 14) && localX is >= 18 and <= 21)
            {
                return HybridWallTreatmentMark.DetailDark;
            }
            if ((localY is 5 or 12) && localX is >= 6 and <= 7 ||
                (localY is 8 or 15) && localX is >= 19 and <= 20)
            {
                return HybridWallTreatmentMark.DetailLight;
            }
        }

        if (family == ThinWallMaterialFamily.Metal &&
            surface is HybridWallRasterSurface.Front or HybridWallRasterSurface.WestSide or HybridWallRasterSurface.EastSide)
        {
            int localX = PositiveMod(x, 30);
            int localY = PositiveMod(y, 20);
            int panelY = localY % 10;
            int staggeredSeamX = localY < 10 ? 20 : 10;
            if (localX == staggeredSeamX && panelY is >= 1 and <= 8 || localY == 10 ||
                (localX == staggeredSeamX - 3 || localX == staggeredSeamX + 3) &&
                panelY is 2 or 7)
            {
                return HybridWallTreatmentMark.DetailDark;
            }
            if ((localX == staggeredSeamX - 2 || localX == staggeredSeamX + 2) &&
                panelY is 2 or 7)
            {
                return HybridWallTreatmentMark.DetailLight;
            }
        }

        return HybridWallTreatmentMark.None;
    }

    private static HybridWallTreatmentMark Damage(
        ThinWallMaterialFamily family,
        ThinWallDamageGrade damage,
        HybridWallRasterPixel pixel,
        HybridWallRayMask rays)
    {
        int grade = (int)damage;
        HybridWallRasterSurface surface = pixel.Surface;
        if (grade <= 0 || surface < HybridWallRasterSurface.WestSide)
        {
            return HybridWallTreatmentMark.None;
        }

        bool horizontalTop = surface == HybridWallRasterSurface.Top &&
                             (pixel.OwnerRays & (HybridWallRayMask.East | HybridWallRayMask.West)) != 0;
        int along;
        int cross;
        int crossSpan;
        switch (surface)
        {
            case HybridWallRasterSurface.Front:
                along = pixel.SourceX;
                cross = pixel.SourceY - 3;
                crossSpan = HybridWallRasterCompiler.FrontDepth;
                break;
            case HybridWallRasterSurface.WestSide:
                along = pixel.SourceY;
                cross = pixel.SourceX - 3;
                crossSpan = HybridWallRasterCompiler.WestSideDepth;
                break;
            case HybridWallRasterSurface.EastSide:
                along = pixel.SourceY;
                cross = pixel.SourceX - 47;
                crossSpan = HybridWallRasterCompiler.EastSideDepth;
                break;
            default:
                along = horizontalTop ? pixel.SourceX : pixel.SourceY;
                int sourceCross = horizontalTop ? pixel.SourceY - 25 : pixel.SourceX - 14;
                cross = Math.Max(0, Math.Min(6, (int)Math.Round(sourceCross * (6f / 32f))));
                crossSpan = HybridWallRasterCompiler.StandardTopWidth;
                break;
        }

        if (grade >= 2 && surface == HybridWallRasterSurface.Top)
        {
            int topCenter = family switch
            {
                ThinWallMaterialFamily.Wood => 35,
                ThinWallMaterialFamily.Metal => 40,
                _ => 31,
            };
            int dx = along - topCenter;
            int dy = cross - 3;
            if (grade >= 3 && Math.Abs(dx) <= 1 && dy == 0)
            {
                return HybridWallTreatmentMark.DamageVoid;
            }
            if (dy == 1 && dx is >= -4 and <= 3)
            {
                return HybridWallTreatmentMark.DamageLight;
            }
            if (dy == 0 && dx is >= -5 and <= 5 && dx != 4)
            {
                return grade >= 3 && Math.Abs(dx) <= 2
                    ? HybridWallTreatmentMark.DamageChip
                    : HybridWallTreatmentMark.DamageDark;
            }
        }
        if (surface == HybridWallRasterSurface.Top)
        {
            return HybridWallTreatmentMark.None;
        }

        return family switch
        {
            ThinWallMaterialFamily.Wood => WoodDamage(grade, along, cross, crossSpan),
            ThinWallMaterialFamily.Metal => MetalDamage(grade, along, cross, crossSpan),
            _ => StoneDamage(grade, along, cross, crossSpan),
        };

        HybridWallTreatmentMark StoneDamage(int strength, int longitudinal, int transverse, int span)
        {
            int middle = Math.Max(1, (span - 1) / 2);
            if (strength >= 3)
            {
                int chipDx = longitudinal - 21;
                int chipDy = transverse - middle;
                if (Math.Abs(chipDx) <= 1 && chipDy is 0 or 1)
                {
                    return HybridWallTreatmentMark.DamageVoid;
                }
                if (chipDy == 2 && chipDx is >= -3 and <= 2)
                {
                    return HybridWallTreatmentMark.DamageLight;
                }
                bool chip = chipDx is >= -3 and <= 3 && chipDy is >= -2 and <= 2 &&
                            !(Math.Abs(chipDx) == 3 && Math.Abs(chipDy) == 2);
                if (chip)
                {
                    return HybridWallTreatmentMark.DamageChip;
                }
            }

            HybridWallTreatmentMark primary = ChippedCrack(
                longitudinal, transverse, 21, middle, mirrored: false);
            if (primary != HybridWallTreatmentMark.None)
            {
                return primary;
            }
            if (strength >= 2)
            {
                HybridWallTreatmentMark secondary = ChippedCrack(
                    longitudinal,
                    transverse,
                    39,
                    Math.Max(2, middle - 2),
                    mirrored: true);
                if (secondary != HybridWallTreatmentMark.None)
                {
                    return secondary;
                }
            }
            if (strength >= 3)
            {
                HybridWallTreatmentMark tertiary = ChippedCrack(
                    longitudinal,
                    transverse,
                    29,
                    Math.Min(span - 2, middle + 2),
                    mirrored: false);
                if (tertiary != HybridWallTreatmentMark.None)
                {
                    return tertiary;
                }
            }
            return HybridWallTreatmentMark.None;

            HybridWallTreatmentMark ChippedCrack(
                int x,
                int y,
                int centerX,
                int centerY,
                bool mirrored)
            {
                int localY = y - centerY;
                if (localY < -3 || localY > 3)
                {
                    return HybridWallTreatmentMark.None;
                }

                int bend = localY switch
                {
                    <= -2 => -1,
                    <= 0 => 0,
                    <= 2 => 1,
                    _ => 2,
                };
                if (mirrored)
                {
                    bend = -bend;
                }
                int targetX = centerX + bend;
                int dx = x - centerX;
                bool upperBranch = localY == -2 && dx is >= -5 and <= -2;
                bool lowerBranch = localY == 2 && dx is >= 2 and <= 5;
                if (mirrored)
                {
                    upperBranch = localY == -2 && dx is >= 2 and <= 5;
                    lowerBranch = localY == 2 && dx is >= -5 and <= -2;
                }
                if (x == targetX || x == targetX + (mirrored ? -1 : 1) || upperBranch || lowerBranch)
                {
                    return HybridWallTreatmentMark.DamageDark;
                }

                bool spineHighlight = x == targetX + (mirrored ? -2 : 2);
                bool upperHighlight = localY == -1 && (mirrored ? dx is >= 2 and <= 5 : dx is >= -5 and <= -2);
                bool lowerHighlight = localY == 3 && (mirrored ? dx is >= -5 and <= -2 : dx is >= 2 and <= 5);
                return spineHighlight || upperHighlight || lowerHighlight
                    ? HybridWallTreatmentMark.DamageLight
                    : HybridWallTreatmentMark.None;
            }
        }

        HybridWallTreatmentMark WoodDamage(int strength, int longitudinal, int transverse, int span)
        {
            int middle = Math.Max(1, (span - 1) / 2);
            (int X, int Y)[] tears =
            {
                (13, middle),
                (35, Math.Max(1, middle - 2)),
                (47, Math.Min(span - 2, middle + 2)),
            };
            for (int index = 0; index < strength; index++)
            {
                int dx = longitudinal - tears[index].X;
                int dy = transverse - tears[index].Y;
                int halfLength = 5;
                if (strength >= 3 && index == 0 && Math.Abs(dx) <= 1 && dy == 0)
                {
                    return HybridWallTreatmentMark.DamageVoid;
                }
                if (dy == 1 && dx >= -halfLength && dx <= halfLength - 1)
                {
                    return HybridWallTreatmentMark.DamageLight;
                }
                bool gash = dy == 0 && dx >= -halfLength && dx <= halfLength && dx != halfLength - 1;
                bool upperSplinter = (dy == -2 || index > 0 && dy == -3) &&
                                     dx is >= -4 and <= 2;
                bool lowerSplinter = (dy == 3 || index > 0 && dy == 2) &&
                                     dx is >= 0 and <= 5;
                if (gash || upperSplinter || lowerSplinter)
                {
                    return strength >= 3 && gash && Math.Abs(dx) <= 1
                        ? HybridWallTreatmentMark.DamageChip
                        : HybridWallTreatmentMark.DamageDark;
                }
            }
            return HybridWallTreatmentMark.None;
        }

        HybridWallTreatmentMark MetalDamage(int strength, int longitudinal, int transverse, int span)
        {
            int middle = Math.Max(1, (span - 1) / 2);
            (int X, int Y)[] dents =
            {
                (15, middle),
                (39, Math.Max(1, middle - 2)),
                (49, Math.Min(span - 2, middle + 2)),
            };
            for (int index = 0; index < strength; index++)
            {
                int dx = longitudinal - dents[index].X;
                int dy = transverse - dents[index].Y;
                int radiusSquared = dx * dx + dy * dy;
                int radius = 4 + Math.Min(index, 1);
                if (strength >= 3 && index == 2 && Math.Abs(dx) <= 1 && dy == 0)
                {
                    return HybridWallTreatmentMark.DamageVoid;
                }
                if (dx >= 2 && dy >= 0 && radiusSquared <= radius * radius &&
                    radiusSquared >= (radius - 2) * (radius - 2))
                {
                    return HybridWallTreatmentMark.DamageLight;
                }
                bool depression = radiusSquared <= (radius - 1) * (radius - 1) &&
                                  !(dx <= -2 && dy >= 2);
                bool scrape = dy == -radius && dx >= -2 && dx <= radius + 1;
                if (depression || scrape)
                {
                    return HybridWallTreatmentMark.DamageDark;
                }
            }
            return HybridWallTreatmentMark.None;
        }
    }

    private static int PositiveMod(int value, int modulus)
    {
        int result = value % modulus;
        return result < 0 ? result + modulus : result;
    }
}
