using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

[StaticConstructorOnStartup]
public static class HybridWallRenderer
{
    private const float CompletedAltitude = 0.006f;
    private static readonly Color32 LowShadowVertexColor = new(0, 0, 0, 0);
    private static readonly Color32 HighShadowVertexColor = new(255, 0, 0, 255);
    private static readonly Dictionary<string, Material> SlicedRealtimeMaterials = new();

    public static void DrawStateEdge(
        OwnedEdge edge,
        int ownerCount,
        Material stateMaterial,
        float altitude)
    {
        SharedEdge shared = edge.Shared;
        bool horizontal = shared.PositiveSide == ThinWallSide.North;
        HybridWallRayMask rays = horizontal
            ? HybridWallRayMask.East | HybridWallRayMask.West
            : HybridWallRayMask.North | HybridWallRayMask.South;
        Material compiled = CoreDerivedWallMaterialCache.StateMaterial(
            rays,
            ownerCount > 1 ? rays : HybridWallRayMask.None,
            stateMaterial.color);
        Vector3 center = ThinWallRenderGeometry.StructuralCenter(edge, altitude);
        Graphics.DrawMesh(
            MeshPool.plane10,
            Matrix4x4.TRS(
                center,
                Quaternion.identity,
                horizontal
                    ? new Vector3(ThinWallRenderGeometry.UnionPlaneScale, 1f, 1f)
                    : new Vector3(1f, 1f, ThinWallRenderGeometry.UnionPlaneScale)),
            compiled,
            0);
    }

    public static void DrawDoor(Building_ThinDoor door, float openFraction)
    {
        SharedEdge edge = door.OwnedEdge.Shared;
        bool horizontal = edge.PositiveSide == ThinWallSide.North;
        Vector3 structuralCenter = ThinWallRenderGeometry.StructuralCenter(door.OwnedEdge, 0f);
        float centerX = structuralCenter.x;
        float centerZ = structuralCenter.z;
        HybridWallRayMask rays = horizontal
            ? HybridWallRayMask.East | HybridWallRayMask.West
            : HybridWallRayMask.North | HybridWallRayMask.South;
        HybridWallRuntimePlan runtimePlan = HybridWallRuntimePlanCompiler.Compile(
            HybridWallVertexTopology.FromOccupancy(HybridWallQuadrant.None, rays),
            HybridWallRayMask.None,
            HybridWallRayMask.None);
        Material leaf = CoreDerivedWallMaterialCache.RuntimeMaterial(
            CoreWallAtlasMaterial(door),
            runtimePlan,
            FamilyFor(door),
            ThinWallVisualResolver.DamageGrade(door.HitPoints, door.MaxHitPoints),
            door: true);
        leaf = CoreDerivedWallMaterialCache.RealtimeMaterial(leaf);
        HybridWallDoorPlan plan = HybridWallDoorGeometryCompiler.CompileVisible(openFraction);
        HybridWallDoorShadowPlan shadow = HybridWallDoorGeometryCompiler.CompileShadow(openFraction);
        float nativeWallAltitude = AltitudeLayer.Building.AltitudeFor();
        float completedWallAltitude = nativeWallAltitude + CompletedAltitude;
        float leafAltitude = HybridWallDoorGeometryCompiler.LeafAltitude(
            nativeWallAltitude,
            completedWallAltitude,
            openFraction);

        if (plan.IsFullyClosed)
        {
            DrawRealtimeSunShadow(HybridWallSunShadowGeometry.ForEdge(edge, ownerCount: 1));
            if (horizontal)
            {
                DrawRealtimeRect(centerX + plan.Left.Min, centerZ - 0.5f,
                    centerX + plan.Right.Max, centerZ + 0.5f,
                    leafAltitude, leaf, new HybridWallUvRect(
                        plan.Left.UvMin,
                        0f,
                        plan.Right.UvMax - plan.Left.UvMin,
                        1f));
            }
            else
            {
                DrawRealtimeRect(centerX - 0.5f, centerZ + plan.Left.Min,
                    centerX + 0.5f, centerZ + plan.Right.Max,
                    leafAltitude, leaf, new HybridWallUvRect(
                        0f,
                        plan.Left.UvMin,
                        1f,
                        plan.Right.UvMax - plan.Left.UvMin));
            }
            return;
        }

        if (horizontal)
        {
            DrawRealtimeSunShadow(HybridWallSunShadowGeometry.ForEdgeSpan(
                edge, 1, shadow.LeftMin + 0.5f, shadow.LeftMax + 0.5f));
            DrawRealtimeSunShadow(HybridWallSunShadowGeometry.ForEdgeSpan(
                edge, 1, shadow.RightMin + 0.5f, shadow.RightMax + 0.5f));
            DrawRealtimeRect(centerX + plan.Left.Min, centerZ - 0.5f,
                centerX + plan.Left.Max, centerZ + 0.5f,
                leafAltitude, leaf, new HybridWallUvRect(
                    plan.Left.UvMin, 0f, plan.Left.UvMax - plan.Left.UvMin, 1f));
            DrawRealtimeRect(centerX + plan.Right.Min, centerZ - 0.5f,
                centerX + plan.Right.Max, centerZ + 0.5f,
                leafAltitude, leaf, new HybridWallUvRect(
                    plan.Right.UvMin, 0f, plan.Right.UvMax - plan.Right.UvMin, 1f));
        }
        else
        {
            DrawRealtimeSunShadow(HybridWallSunShadowGeometry.ForEdgeSpan(
                edge, 1, shadow.LeftMin + 0.5f, shadow.LeftMax + 0.5f));
            DrawRealtimeSunShadow(HybridWallSunShadowGeometry.ForEdgeSpan(
                edge, 1, shadow.RightMin + 0.5f, shadow.RightMax + 0.5f));
            DrawRealtimeRect(centerX - 0.5f, centerZ + plan.Left.Min,
                centerX + 0.5f, centerZ + plan.Left.Max,
                leafAltitude, leaf, new HybridWallUvRect(
                    0f, plan.Left.UvMin, 1f, plan.Left.UvMax - plan.Left.UvMin));
            DrawRealtimeRect(centerX - 0.5f, centerZ + plan.Right.Min,
                centerX + 0.5f, centerZ + plan.Right.Max,
                leafAltitude, leaf, new HybridWallUvRect(
                    0f, plan.Right.UvMin, 1f, plan.Right.UvMax - plan.Right.UvMin));
        }
    }

    private static void DrawRealtimeSunShadow(HybridWallSunShadowVolume volume)
    {
        if (!DebugViewSettings.drawShadows || Find.CurrentMap?.Biome?.disableShadows == true)
        {
            return;
        }

        Mesh mesh = ShadowMeshPool.GetShadowMesh(volume.SizeX, volume.SizeZ, volume.Height);
        Graphics.DrawMesh(
            mesh,
            new Vector3(volume.CenterX, AltitudeLayer.Shadows.AltitudeFor(), volume.CenterZ),
            Quaternion.identity,
            MatBases.SunShadowFade,
            0);
    }

    private static void DrawRealtimeRect(
        float minX,
        float minZ,
        float maxX,
        float maxZ,
        float altitude,
        Material material,
        HybridWallUvRect? uv)
    {
        Vector3 center = new((minX + maxX) * 0.5f, altitude, (minZ + maxZ) * 0.5f);
        Vector3 scale = new(maxX - minX, 1f, maxZ - minZ);
        Material drawMaterial = uv.HasValue ? SlicedRealtimeMaterial(material, uv.Value) : material;
        Graphics.DrawMesh(
            MeshPool.plane10,
            Matrix4x4.TRS(center, Quaternion.identity, scale),
            drawMaterial,
            0);
    }

    private static Material SlicedRealtimeMaterial(Material source, HybridWallUvRect uv)
    {
        string key = FormattableString.Invariant(
            $"{source.GetInstanceID()}:{uv.X:R}:{uv.Y:R}:{uv.Width:R}:{uv.Height:R}");
        if (SlicedRealtimeMaterials.TryGetValue(key, out Material cached))
        {
            return cached;
        }

        var material = new Material(source)
        {
            mainTextureScale = new Vector2(uv.Width, uv.Height),
            mainTextureOffset = new Vector2(uv.X, uv.Y),
            hideFlags = HideFlags.HideAndDontSave,
        };
        SlicedRealtimeMaterials[key] = material;
        return material;
    }

    public static void PrintCompleted(SectionLayer layer, Building building)
    {
        if (building is not IThinEdgeStructure structure)
        {
            return;
        }

        Building[] owners = ThinWallUtility
            .ThingsOnSharedEdge(building.Map, structure.OwnedEdge.Shared, completedOnly: true)
            .OfType<Building>()
            .OrderBy(candidate => candidate.thingIDNumber)
            .ToArray();
        if (owners.Length == 0 || !ReferenceEquals(owners[0], building))
        {
            return;
        }

        PrintVertexIfCanonical(layer, building, FirstEndpoint(structure.OwnedEdge.Shared));
        PrintVertexIfCanonical(layer, building, SecondEndpoint(structure.OwnedEdge.Shared));
    }

    private static void PrintVertexIfCanonical(
        SectionLayer layer,
        Building building,
        IntVec3 vertex)
    {
        VertexContext context = ResolveContext(building.Map, vertex);
        Building? canonical = context.ThinSources.Values
            .SelectMany(sources => sources)
            .Distinct()
            .OrderBy(candidate => candidate.thingIDNumber)
            .FirstOrDefault();
        if (!ReferenceEquals(canonical, building) || context.Topology.ThinRays == HybridWallRayMask.None)
        {
            return;
        }

        HybridWallRayMask wallShadowRays = context.Topology.ThinRays & ~context.DoorRays;
        if (wallShadowRays != HybridWallRayMask.None)
        {
            PrintThinVertexShadow(
                layer,
                vertex,
                HybridThinShadowCompiler.Compile(
                    wallShadowRays,
                    context.DoubledRays & wallShadowRays));
        }

        PrintVertex(layer, context);
    }

    private static void PrintVertex(
        SectionLayer layer,
        VertexContext context)
    {
        HybridWallRayMask regularOwned = HybridRegularContactOwnership.AllRegularOwnedRays(
            context.Topology.OrdinaryQuadrants,
            context.Topology.ThinRays);
        HybridWallRayMask vertexRays = context.Topology.ThinRays & ~regularOwned;
        if (vertexRays == HybridWallRayMask.None)
        {
            return;
        }
        HybridWallVertexTopology vertexTopology = HybridWallVertexTopology.FromOccupancy(
            context.Topology.OrdinaryQuadrants,
            vertexRays);
        HybridWallRayMask vertexDoorRays = context.DoorRays & vertexRays;
        HybridWallRuntimePlan plan = HybridWallRuntimePlanCompiler.Compile(
            vertexTopology,
            context.DoubledRays & vertexRays,
            vertexDoorRays);
        float wallAltitude = AltitudeLayer.Building.AltitudeFor() + CompletedAltitude;
        float frameAltitude = HybridWallDoorGeometryCompiler.ThinFrameAltitude(wallAltitude);
        Vector3 center = ThinWallRenderGeometry.StructuralVertexCenter(context.Vertex, wallAltitude);

        var partitions = new List<VertexPartition>();
        foreach (HybridWallDirection direction in context.Topology.Arms.OccupiedDirections)
        {
            if (!context.ThinSources.TryGetValue(direction, out IReadOnlyList<Building> sources))
            {
                continue;
            }

            HybridWallRayMask ray = Mask(direction);
            if (regularOwned.HasFlag(ray))
            {
                continue;
            }
            bool doubled = sources.Count > 1;
            foreach (Building source in sources)
            {
                Material sourceMaterial = CoreWallAtlasMaterial(source);
                partitions.Add(new VertexPartition(
                    ray,
                    source,
                    sourceMaterial,
                    FamilyFor(source),
                    ThinWallVisualResolver.DamageGrade(source.HitPoints, source.MaxHitPoints),
                    doubled,
                    doubled && source is IThinEdgeStructure edgeSource
                        ? edgeSource.OwnedEdge.Side
                        : null));
            }
        }
        if (partitions.Count == 0)
        {
            return;
        }

        Material outline = CoreDerivedWallMaterialCache.RuntimeMaterial(
            partitions[0].SourceMaterial,
            plan,
            partitions[0].Family,
            ThinWallDamageGrade.None,
            door: false,
            outlineOnly: true);
        PrintPlane(outline,
            HybridWallDoorGeometryCompiler.HasThinOwnedFrame(context.DoorRays, vertexRays)
                ? frameAltitude
                : wallAltitude);

        HybridWallPartitionVisual[] visualPartitions = partitions
            .Select(partition => new HybridWallPartitionVisual(
                partition.Ray,
                partition.SourceMaterial.GetInstanceID(),
                partition.Family,
                partition.Damage,
                partition.Doubled,
                partition.Source is Building_ThinDoor))
            .ToArray();
        if (HybridWallPartitionBatching.CanRenderAsOneStructuralUnion(visualPartitions))
        {
            VertexPartition first = partitions[0];
            HybridWallRayMask combinedRays = partitions.Aggregate(
                HybridWallRayMask.None,
                (mask, partition) => mask | partition.Ray);
            Material material = CoreDerivedWallMaterialCache.RuntimeMaterial(
                first.SourceMaterial,
                plan,
                first.Family,
                first.Damage,
                door: false,
                partitionRay: combinedRays);
            PrintPlane(material, wallAltitude + 0.001f);
        }
        else
        {
            foreach (VertexPartition partition in partitions)
            {
                Material material = CoreDerivedWallMaterialCache.RuntimeMaterial(
                    partition.SourceMaterial,
                    plan,
                    partition.Family,
                    partition.Damage,
                    door: false,
                    partitionRay: partition.Ray,
                    ownerSide: partition.OwnerSide);
                PrintPlane(material,
                    partition.Source is Building_ThinDoor ? frameAltitude : wallAltitude + 0.001f);
            }
        }

        void PrintPlane(Material material, float altitude)
        {
            Printer_Plane.PrintPlane(
                layer,
                new Vector3(center.x, altitude, center.z),
                new Vector2(
                    ThinWallRenderGeometry.UnionPlaneScale,
                    ThinWallRenderGeometry.UnionPlaneScale),
                material,
                topVerticesAltitudeBias: HybridWallDoorGeometryCompiler.CustomPlaneTopVerticesAltitudeBias);
        }
    }

    internal static bool TryPrintHybridRegularWall(
        SectionLayer layer,
        Building wall,
        Material nativeLinkedMaterial)
    {
        if (wall.Map == null || wall.def.building?.isWall != true)
        {
            return false;
        }

        var corners = new List<HybridWallCornerRaster>(4);
        var visuals = new List<HybridWallContactVisual>(8);
        var sourceMaterials = new Dictionary<int, Material>();
        AddCorner(wall.Position, HybridWallQuadrant.NorthEast);
        AddCorner(new IntVec3(wall.Position.x + 1, 0, wall.Position.z), HybridWallQuadrant.NorthWest);
        AddCorner(new IntVec3(wall.Position.x, 0, wall.Position.z + 1), HybridWallQuadrant.SouthEast);
        AddCorner(new IntVec3(wall.Position.x + 1, 0, wall.Position.z + 1), HybridWallQuadrant.SouthWest);
        if (corners.Count == 0)
        {
            return false;
        }
        if (!HybridWallAtlasSupport.IsSupported(
                nativeLinkedMaterial.mainTexture.name,
                nativeLinkedMaterial.mainTexture.width,
                nativeLinkedMaterial.mainTexture.height) ||
            !CoreDerivedWallMaterialCache.SupportsHybridAtlas(nativeLinkedMaterial.mainTexture))
        {
            return false;
        }

        HybridRegularRenderMaterial render = CoreDerivedWallMaterialCache.HybridRegularMaterial(
            nativeLinkedMaterial,
            LinkIndex(nativeLinkedMaterial),
            corners,
            visuals,
            sourceMaterials);
        Printer_Plane.PrintPlane(
            layer,
            GenThing.TrueCenter(wall),
            Vector2.one,
            render.NativeMaterial,
            topVerticesAltitudeBias: 0.01f);
        foreach (HybridRegularExteriorRenderMaterial exterior in render.ExteriorMaterials)
        {
            PrintHybridRegularExterior(
                layer,
                GenThing.TrueCenter(wall),
                exterior);
        }
        PrintHybridRegularShadow(layer, wall, render.Shadow);
        return true;

        void AddCorner(IntVec3 vertex, HybridWallQuadrant wallQuadrant)
        {
            VertexContext context = ResolveContext(wall.Map, vertex);
            HybridWallRayMask participatingRays = HybridRegularContactOwnership.RaysParticipatingIn(
                wallQuadrant,
                context.Topology.OrdinaryQuadrants,
                context.Topology.ThinRays);
            if (participatingRays == HybridWallRayMask.None ||
                !context.Topology.OrdinaryQuadrants.HasFlag(wallQuadrant))
            {
                return;
            }
            HybridWallRayMask gutterRays = HybridRegularContactOwnership.RaysOwnedBy(
                wallQuadrant,
                context.Topology.OrdinaryQuadrants,
                context.Topology.ThinRays);
            HybridWallRayMask sideTRays = HybridRegularContactOwnership.SideTRaysParticipatingIn(
                wallQuadrant,
                context.Topology.OrdinaryQuadrants,
                context.Topology.ThinRays);
            corners.Add(new HybridWallCornerRaster(
                wallQuadrant,
                participatingRays,
                context.DoubledRays & participatingRays,
                context.DoorRays & participatingRays,
                gutterRays,
                sideTRays));
            foreach (HybridWallDirection direction in new[]
                     {
                         HybridWallDirection.North,
                         HybridWallDirection.East,
                         HybridWallDirection.South,
                         HybridWallDirection.West,
                     })
            {
                HybridWallRayMask ray = Mask(direction);
                if (!participatingRays.HasFlag(ray) ||
                    !context.ThinSources.TryGetValue(direction, out IReadOnlyList<Building> sources))
                {
                    continue;
                }
                bool doubled = sources.Count > 1;
                foreach (Building source in sources)
                {
                    Material sourceMaterial = CoreWallAtlasMaterial(source);
                    int materialId = sourceMaterial.GetInstanceID();
                    sourceMaterials[materialId] = sourceMaterial;
                    visuals.Add(new HybridWallContactVisual(
                        new HybridWallContactKey(wallQuadrant, ray),
                        materialId,
                        FamilyFor(source),
                        ThinWallVisualResolver.DamageGrade(source.HitPoints, source.MaxHitPoints),
                        source is Building_ThinDoor,
                        doubled && source is IThinEdgeStructure edgeSource
                            ? edgeSource.OwnedEdge.Side
                            : null));
                }
            }
        }
    }

    private static void PrintHybridRegularExterior(
        SectionLayer layer,
        Vector3 wallCenter,
        HybridRegularExteriorRenderMaterial exterior)
    {
        HybridRegularExteriorRegion region = exterior.Region;
        Printer_Plane.PrintPlane(
            layer,
            new Vector3(
                wallCenter.x + region.CenterX,
                wallCenter.y + region.AltitudeOffset,
                wallCenter.z + region.CenterZ),
            new Vector2(region.SizeX, region.SizeZ),
            exterior.Material,
            topVerticesAltitudeBias: region.TopVerticesAltitudeBias);
    }

    private static void PrintHybridRegularShadow(
        SectionLayer layer,
        Building wall,
        HybridRegularShadowPlan shadow)
    {
        if (!DebugViewSettings.drawShadows)
        {
            return;
        }

        PrintRasterShadow(layer, wall.Position.x, wall.Position.z, shadow);
    }

    private static void PrintThinVertexShadow(
        SectionLayer layer,
        IntVec3 vertex,
        HybridRegularShadowPlan shadow)
    {
        if (!DebugViewSettings.drawShadows)
        {
            return;
        }

        PrintRasterShadow(layer, vertex.x - 0.5f, vertex.z - 0.5f, shadow);
    }

    private static void PrintRasterShadow(
        SectionLayer layer,
        float originX,
        float originZ,
        HybridRegularShadowPlan shadow)
    {
        LayerSubMesh subMesh = layer.GetSubMesh(MatBases.SunShadowFade);
        float unit = 1f / HybridWallRasterPlan.Size;
        foreach (HybridWallShadowRun run in shadow.Runs)
        {
            float minX = originX + run.MinX * unit;
            float maxX = originX + run.MaxX * unit;
            float minZ = originZ + run.Y * unit;
            float maxZ = minZ + unit;
            int first = subMesh.verts.Count;
            subMesh.verts.Add(new Vector3(minX, 0f, minZ));
            subMesh.verts.Add(new Vector3(minX, 0f, maxZ));
            subMesh.verts.Add(new Vector3(maxX, 0f, maxZ));
            subMesh.verts.Add(new Vector3(maxX, 0f, minZ));
            for (int index = 0; index < 4; index++)
            {
                subMesh.colors.Add(LowShadowVertexColor);
            }
            subMesh.tris.Add(first);
            subMesh.tris.Add(first + 1);
            subMesh.tris.Add(first + 2);
            subMesh.tris.Add(first);
            subMesh.tris.Add(first + 2);
            subMesh.tris.Add(first + 3);
        }

        foreach (HybridWallShadowEdge edge in shadow.CastingEdges)
        {
            bool reverse = edge.CastingSide == HybridWallShadowCastingSide.East;
            Vector3 start = new(
                originX + (reverse ? edge.MaxX : edge.MinX) * unit,
                0f,
                originZ + (reverse ? edge.MaxY : edge.MinY) * unit);
            Vector3 end = new(
                originX + (reverse ? edge.MinX : edge.MaxX) * unit,
                0f,
                originZ + (reverse ? edge.MinY : edge.MaxY) * unit);
            int first = subMesh.verts.Count;
            subMesh.verts.Add(start);
            subMesh.verts.Add(end);
            subMesh.verts.Add(start);
            subMesh.verts.Add(end);
            subMesh.colors.Add(LowShadowVertexColor);
            subMesh.colors.Add(LowShadowVertexColor);
            subMesh.colors.Add(HighShadowVertexColor);
            subMesh.colors.Add(HighShadowVertexColor);
            foreach (int triangleIndex in HybridWallShadowMeshTopology.TriangleIndices(edge.CastingSide))
            {
                subMesh.tris.Add(first + triangleIndex);
            }
        }
    }

    private static int LinkIndex(Material nativeLinkedMaterial)
    {
        const float gutterOffset = 1f / 32f;
        int column = Mathf.Clamp(
            Mathf.RoundToInt((nativeLinkedMaterial.mainTextureOffset.x - gutterOffset) / 0.25f),
            0,
            3);
        int row = Mathf.Clamp(
            Mathf.RoundToInt((nativeLinkedMaterial.mainTextureOffset.y - gutterOffset) / 0.25f),
            0,
            3);
        return row * 4 + column;
    }

    private static ThinWallMaterialFamily FamilyFor(Building source) =>
        ThinWallVisualResolver.ResolveMaterialFamily(
            source.Stuff?.stuffProps?.categories.Select(category => category.defName));

    internal static Material CoreWallAtlasMaterial(Building source)
    {
        ThingDef graphicDef = source.def.building?.isWall == true ? source.def : ThingDefOf.Wall;
        Graphic graphic = graphicDef.graphicData.Graphic;
        if (!graphicDef.graphicData.ignoreThingDrawColor)
        {
            graphic = graphic.GetColoredVersion(graphic.Shader, source.DrawColor, source.DrawColorTwo);
        }

        while (graphic is Graphic_Linked linked)
        {
            graphic = linked.SubGraphic;
        }
        if (graphic is Graphic_Appearances appearances)
        {
            graphic = appearances.SubGraphicFor(source.Stuff);
        }

        Material material = graphic.MatSingleFor(source);
        if (material.mainTexture is Texture2D texture && HybridWallAtlasSupport.IsSupported(
                texture.name,
                texture.width,
                texture.height))
        {
            return material;
        }

        Graphic core = ThingDefOf.Wall.graphicData.Graphic;
        if (!ThingDefOf.Wall.graphicData.ignoreThingDrawColor)
        {
            core = core.GetColoredVersion(core.Shader, source.DrawColor, source.DrawColorTwo);
        }
        while (core is Graphic_Linked linkedCore)
        {
            core = linkedCore.SubGraphic;
        }
        if (core is Graphic_Appearances coreAppearances)
        {
            core = coreAppearances.SubGraphicFor(source.Stuff);
        }
        return core.MatSingleFor(source);
    }

    private static VertexContext ResolveContext(Map map, IntVec3 vertex)
    {
        var thinSources = new Dictionary<HybridWallDirection, IReadOnlyList<Building>>();
        HybridWallRayMask thinMask = HybridWallRayMask.None;
        HybridWallRayMask doubledRays = HybridWallRayMask.None;
        HybridWallRayMask doorRays = HybridWallRayMask.None;
        foreach ((HybridWallDirection direction, SharedEdge edge) in IncidentEdges(vertex))
        {
            Building[] sources = ThinWallUtility.ThingsOnSharedEdge(map, edge, completedOnly: true)
                .OfType<Building>()
                .OrderBy(candidate => candidate.thingIDNumber)
                .ToArray();
            if (sources.Length == 0)
            {
                continue;
            }

            thinSources[direction] = sources;
            thinMask |= Mask(direction);
            if (sources.Any(source => source is Building_ThinDoor))
            {
                doorRays |= Mask(direction);
            }
            if (sources.Length > 1)
            {
                doubledRays |= Mask(direction);
            }
        }

        HybridWallQuadrant quadrants = HybridWallQuadrant.None;
        AddQuadrant(HybridWallQuadrant.NorthEast, new IntVec3(vertex.x, 0, vertex.z));
        AddQuadrant(HybridWallQuadrant.NorthWest, new IntVec3(vertex.x - 1, 0, vertex.z));
        AddQuadrant(HybridWallQuadrant.SouthEast, new IntVec3(vertex.x, 0, vertex.z - 1));
        AddQuadrant(HybridWallQuadrant.SouthWest, new IntVec3(vertex.x - 1, 0, vertex.z - 1));

        return new VertexContext(
            vertex,
            HybridWallVertexTopology.FromOccupancy(quadrants, thinMask),
            doubledRays,
            doorRays,
            thinSources);

        void AddQuadrant(HybridWallQuadrant quadrant, IntVec3 cell)
        {
            if (!cell.InBounds(map))
            {
                return;
            }
            if (cell.GetThingList(map).OfType<Building>().Any(candidate =>
                    candidate.def.building?.isWall == true && SupportsHybridRegularWall(candidate)))
            {
                quadrants |= quadrant;
            }
        }
    }

    private static bool SupportsHybridRegularWall(Building wall)
    {
        Graphic graphic = wall.Graphic;
        while (graphic is Graphic_Linked linked)
        {
            graphic = linked.SubGraphic;
        }
        if (graphic is Graphic_Appearances appearances)
        {
            graphic = appearances.SubGraphicFor(wall.Stuff);
        }
        Material material = graphic.MatSingleFor(wall);
        return HybridWallAtlasSupport.IsSupported(
                   material.mainTexture.name,
                   material.mainTexture.width,
                   material.mainTexture.height) &&
               CoreDerivedWallMaterialCache.SupportsHybridAtlas(material.mainTexture);
    }

    private static IEnumerable<(HybridWallDirection Direction, SharedEdge Edge)> IncidentEdges(IntVec3 vertex)
    {
        yield return (HybridWallDirection.West,
            new SharedEdge(new IntVec3(vertex.x - 1, 0, vertex.z - 1), ThinWallSide.North));
        yield return (HybridWallDirection.East,
            new SharedEdge(new IntVec3(vertex.x, 0, vertex.z - 1), ThinWallSide.North));
        yield return (HybridWallDirection.South,
            new SharedEdge(new IntVec3(vertex.x - 1, 0, vertex.z - 1), ThinWallSide.East));
        yield return (HybridWallDirection.North,
            new SharedEdge(new IntVec3(vertex.x - 1, 0, vertex.z), ThinWallSide.East));
    }

    private static HybridWallRayMask Mask(HybridWallDirection direction) => direction switch
    {
        HybridWallDirection.North => HybridWallRayMask.North,
        HybridWallDirection.East => HybridWallRayMask.East,
        HybridWallDirection.South => HybridWallRayMask.South,
        HybridWallDirection.West => HybridWallRayMask.West,
        _ => HybridWallRayMask.None,
    };

    private static IntVec3 FirstEndpoint(SharedEdge edge) =>
        edge.PositiveSide == ThinWallSide.North
            ? new IntVec3(edge.AnchorCell.x, 0, edge.AnchorCell.z + 1)
            : new IntVec3(edge.AnchorCell.x + 1, 0, edge.AnchorCell.z);

    private static IntVec3 SecondEndpoint(SharedEdge edge)
    {
        IntVec3 first = FirstEndpoint(edge);
        return edge.PositiveSide == ThinWallSide.North
            ? new IntVec3(first.x + 1, 0, first.z)
            : new IntVec3(first.x, 0, first.z + 1);
    }

    private sealed class VertexContext
    {
        public VertexContext(
            IntVec3 vertex,
            HybridWallVertexTopology topology,
            HybridWallRayMask doubledRays,
            HybridWallRayMask doorRays,
            IReadOnlyDictionary<HybridWallDirection, IReadOnlyList<Building>> thinSources)
        {
            Vertex = vertex;
            Topology = topology;
            DoubledRays = doubledRays;
            DoorRays = doorRays;
            ThinSources = thinSources;
        }

        public IntVec3 Vertex { get; }
        public HybridWallVertexTopology Topology { get; }
        public HybridWallRayMask DoubledRays { get; }
        public HybridWallRayMask DoorRays { get; }
        public IReadOnlyDictionary<HybridWallDirection, IReadOnlyList<Building>> ThinSources { get; }
    }

    private sealed class VertexPartition
    {
        public VertexPartition(
            HybridWallRayMask ray,
            Building source,
            Material sourceMaterial,
            ThinWallMaterialFamily family,
            ThinWallDamageGrade damage,
            bool doubled,
            ThinWallSide? ownerSide)
        {
            Ray = ray;
            Source = source;
            SourceMaterial = sourceMaterial;
            Family = family;
            Damage = damage;
            Doubled = doubled;
            OwnerSide = ownerSide;
        }

        public HybridWallRayMask Ray { get; }
        public Building Source { get; }
        public Material SourceMaterial { get; }
        public ThinWallMaterialFamily Family { get; }
        public ThinWallDamageGrade Damage { get; }
        public bool Doubled { get; }
        public ThinWallSide? OwnerSide { get; }
    }
}
