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
    private static readonly Color32 LowShadowVertexColor = new(0, 0, 0, 0);
    private static readonly Color32 HighShadowVertexColor = new(255, 0, 0, 255);
    private static readonly HybridWallRayMask[] CardinalRays =
    {
        HybridWallRayMask.North,
        HybridWallRayMask.East,
        HybridWallRayMask.South,
        HybridWallRayMask.West,
    };
    private static readonly Dictionary<string, Material> SlicedRealtimeMaterials = new();
    private static readonly Dictionary<int, Material> RealtimeMaterials = new();
    private static readonly Dictionary<string, Mesh> NativeDoorMeshes = new();

    public static void DrawStateEdge(
        OwnedEdge edge,
        int ownerCount,
        Material stateMaterial,
        float altitude)
    {
        SharedEdge shared = edge.Shared;
        bool horizontal = shared.PositiveSide == ThinWallSide.North;
        Vector3 center = ThinWallRenderGeometry.StructuralCenter(edge, altitude);
        if (NativeWallMeshPrinter.CanRemap(stateMaterial))
        {
            DrawRealtimeNativePlan(center, stateMaterial, NativeWallMeshPlan.Straight(horizontal));
            return;
        }

        // Missing graphics must still be visible, but never expose an entire atlas.
        Graphics.DrawMesh(MeshPool.plane10,
            Matrix4x4.TRS(center, Quaternion.identity,
                horizontal
                    ? new Vector3(1f, 1f, 34f / 60f)
                    : new Vector3(34f / 60f, 1f, 1f)),
            BaseContent.BadMat, 0);
    }

    internal static Material CoreWallStateMaterial(ThingDef buildDef, ThingDef? stuff, Color color)
    {
        return WallMaterial(ThingDefOf.Wall.graphicData.Graphic, stuff, null,
            color, Color.white, ThingDefOf.Wall.graphicData.ignoreThingDrawColor);
    }

    internal static Material BlueprintMaterial(Color color) =>
        WallMaterial(ThingDefOf.Wall.blueprintDef.graphicData.Graphic, null, null,
            color, Color.white, ignoreColor: false);

    private static Material WallMaterial(Graphic graphic, ThingDef? stuff, Thing? thing,
        Color color, Color colorTwo, bool ignoreColor)
    {
        // Graphic_Appearances rejects secondary colors. Resolve the actual native
        // appearance first; the leaf graphic owns the shader/mask and both tints.
        while (true)
        {
            if (graphic is Graphic_Linked linked) graphic = linked.SubGraphic;
            else if (graphic is Graphic_Appearances appearances) graphic = appearances.SubGraphicFor(stuff);
            else break;
        }
        Material material = thing == null ? graphic.MatSingle : graphic.MatSingleFor(thing);
        return ignoreColor ? material : NativeWallMaterial.WithColors(material, color, colorTwo);
    }

    private static void DrawRealtimeNativePlan(
        Vector3 center,
        Material atlas,
        IReadOnlyList<NativeWallMeshQuad> plan)
    {
        foreach (NativeWallMeshQuad quad in plan)
        {
            Material linked = NativeWallMaterial.ForLinks(atlas, (int)quad.SourceLinks);
            Material sliced = SlicedLinkedRealtimeMaterial(linked, quad.Source);
            HybridWallUvRect d = quad.Destination;
            Graphics.DrawMesh(MeshPool.plane10,
                Matrix4x4.TRS(
                    center + new Vector3(d.X + d.Width * 0.5f, 0f, d.Y + d.Height * 0.5f),
                    Quaternion.identity,
                    new Vector3(d.Width, 1f, d.Height)),
                sliced,
                0);
        }
    }

    private static Material SlicedLinkedRealtimeMaterial(Material linked, HybridWallUvRect uv)
    {
        Vector2 scale = linked.mainTextureScale;
        Vector2 offset = linked.mainTextureOffset;
        var absolute = new HybridWallUvRect(
            offset.x + scale.x * uv.X,
            offset.y + scale.y * uv.Y,
            scale.x * uv.Width,
            scale.y * uv.Height);
        return SlicedRealtimeMaterial(linked, absolute);
    }

    public static void DrawDoor(Building_ThinDoor door, float openFraction)
    {
        SharedEdge edge = door.OwnedEdge.Shared;
        bool horizontal = edge.PositiveSide == ThinWallSide.North;
        Vector3 structuralCenter = ThinWallRenderGeometry.StructuralCenter(door.OwnedEdge, 0f);
        NativeDoorMoverPlan plan = NativeDoorMoverPlan.Compile(openFraction);
        HybridWallDoorShadowPlan shadow = HybridWallDoorGeometryCompiler.CompileShadow(openFraction);
        if (plan.OpeningWidth <= 0.00001f)
        {
            DrawRealtimeSunShadow(HybridWallSunShadowGeometry.ForEdge(edge, ownerCount: 1));
        }
        else
        {
            DrawRealtimeSunShadow(HybridWallSunShadowGeometry.ForEdgeSpan(
                edge, 1, shadow.LeftMin + 0.5f, shadow.LeftMax + 0.5f));
            DrawRealtimeSunShadow(HybridWallSunShadowGeometry.ForEdgeSpan(
                edge, 1, shadow.RightMin + 0.5f, shadow.RightMax + 0.5f));
        }

        Material leafMaterial = RealtimeMaterial(CoreDoorMaterial(door));
        float altitude = AltitudeLayer.DoorMoveable.AltitudeFor();
        Vector3 drawOrigin = new(structuralCenter.x, altitude, structuralCenter.z);
        Graphics.DrawMesh(NativeDoorMesh(plan.Left, horizontal), drawOrigin,
            Quaternion.identity, leafMaterial, 0);
        Graphics.DrawMesh(NativeDoorMesh(plan.Right, horizontal), drawOrigin,
            Quaternion.identity, leafMaterial, 0);
        DrawRealtimeDoorDamage(door, structuralCenter, horizontal, openFraction, altitude + 0.002f);
    }

    private static Material CoreDoorMaterial(Building_ThinDoor door)
    {
        return WallMaterial(ThingDefOf.Door.graphicData.Graphic, door.Stuff, door,
            door.DrawColor, door.DrawColorTwo,
            ThingDefOf.Door.graphicData.ignoreThingDrawColor);
    }

    private static Mesh NativeDoorMesh(NativeDoorLeafPlan leaf, bool horizontal)
    {
        string key = FormattableString.Invariant(
            $"{horizontal}:{leaf.LongitudinalMin:R}:{leaf.LongitudinalMax:R}:{leaf.UAtMin:R}:{leaf.UAtMax:R}:{leaf.VMin:R}:{leaf.VMax:R}");
        if (NativeDoorMeshes.TryGetValue(key, out Mesh cached)) return cached;

        float n0 = -NativeDoorMoverPlan.NormalHalfWidth;
        float n1 = NativeDoorMoverPlan.NormalHalfWidth;
        var mesh = new Mesh { name = "ThinWalls_CoreDoorMoverSlice" };
        if (horizontal)
        {
            mesh.vertices = new[]
            {
                new Vector3(leaf.LongitudinalMin, 0f, n0),
                new Vector3(leaf.LongitudinalMin, 0f, n1),
                new Vector3(leaf.LongitudinalMax, 0f, n1),
                new Vector3(leaf.LongitudinalMax, 0f, n0),
            };
            mesh.uv = new[]
            {
                new Vector2(leaf.UAtMin, leaf.VMin),
                new Vector2(leaf.UAtMin, leaf.VMax),
                new Vector2(leaf.UAtMax, leaf.VMax),
                new Vector2(leaf.UAtMax, leaf.VMin),
            };
        }
        else
        {
            mesh.vertices = new[]
            {
                new Vector3(n0, 0f, leaf.LongitudinalMin),
                new Vector3(n0, 0f, leaf.LongitudinalMax),
                new Vector3(n1, 0f, leaf.LongitudinalMax),
                new Vector3(n1, 0f, leaf.LongitudinalMin),
            };
            mesh.uv = new[]
            {
                new Vector2(leaf.UAtMin, leaf.VMin),
                new Vector2(leaf.UAtMax, leaf.VMin),
                new Vector2(leaf.UAtMax, leaf.VMax),
                new Vector2(leaf.UAtMin, leaf.VMax),
            };
        }
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        mesh.UploadMeshData(true);
        NativeDoorMeshes[key] = mesh;
        return mesh;
    }

    private static void DrawRealtimeDoorDamage(
        Building_ThinDoor door,
        Vector3 origin,
        bool horizontal,
        float openFraction,
        float altitude)
    {
        ThinWallDamageGrade grade = ThinWallVisualResolver.DamageGrade(door.HitPoints, door.MaxHitPoints);
        if (grade == ThinWallDamageGrade.None) return;
        var materials = BuildingsDamageSectionLayerUtility.GetScratchMats(door);
        if (materials == null || materials.Count == 0) return;
        float open = Mathf.Clamp01(openFraction);
        float slide = open <= HybridWallDoorGeometryCompiler.ClosedUnionThreshold
            ? 0f
            : open * NativeDoorMoverPlan.MaximumSlide;
        NativeDoorMoverPlan visible = NativeDoorMoverPlan.Compile(open);
        foreach (NativeThinDamageMark mark in NativeThinDamagePlan.Compile(grade, door.thingIDNumber))
        {
            bool left = mark.LongitudinalCenter < 0f;
            float longitudinal = mark.LongitudinalCenter + (left ? -slide : slide);
            NativeDoorLeafPlan leaf = left ? visible.Left : visible.Right;
            if (!NativeDoorDamageClipPlan.TryClip(mark, longitudinal, leaf, out NativeDoorDamageClip clip))
                continue;
            longitudinal = clip.Center;
            Vector3 center = horizontal
                ? new Vector3(origin.x + longitudinal, altitude, origin.z + mark.NormalCenter)
                : new Vector3(origin.x + mark.NormalCenter, altitude, origin.z + longitudinal);
            Material source = materials[mark.CoreScratchIndex % materials.Count];
            Material clipped = SlicedRealtimeMaterial(RealtimeMaterial(source), horizontal
                ? new HybridWallUvRect(clip.SourceMin, 0f, clip.SourceMax - clip.SourceMin, 1f)
                : new HybridWallUvRect(0f, clip.SourceMin, 1f, clip.SourceMax - clip.SourceMin));
            Graphics.DrawMesh(MeshPool.plane10,
                Matrix4x4.TRS(center, Quaternion.identity,
                    horizontal
                        ? new Vector3(clip.Width, 1f, mark.Size)
                        : new Vector3(mark.Size, 1f, clip.Width)),
                clipped, 0);
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

    private static Material RealtimeMaterial(Material source)
    {
        int key = source.GetInstanceID();
        if (RealtimeMaterials.TryGetValue(key, out Material cached)) return cached;
        var material = new Material(source)
        {
            hideFlags = HideFlags.HideAndDontSave,
            name = source.name + "_TW_Realtime",
        };
        // Retain Core's Cutout/CutoutComplex shader and mask channels; only the
        // queue moves after the cached section mesh for a realtime moving leaf.
        material.renderQueue = ShaderDatabase.Transparent.renderQueue;
        RealtimeMaterials[key] = material;
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
        HybridWallRayMask regularOwned = HybridWallRayMask.None;
        if (TryResolveNativeRegularContact(context, out NativeRegularContact admitted))
            regularOwned |= Mask(admitted.Rule.StemDirection);
        HybridWallRayMask vertexRays = context.Topology.ThinRays & ~regularOwned;
        if (vertexRays != HybridWallRayMask.None)
        {
            PrintNativeThinVertex(layer, context, vertexRays);
        }
    }

    private static void PrintNativeThinVertex(
        SectionLayer layer,
        VertexContext context,
        HybridWallRayMask rays)
    {
        Building[] allSources = context.ThinSources
            .Where(entry => rays.HasFlag(Mask(entry.Key)))
            .SelectMany(entry => entry.Value)
            .OrderBy(source => source.thingIDNumber)
            .ToArray();
        if (allSources.Length == 0) return;
        Vector3 center = ThinWallRenderGeometry.StructuralVertexCenter(
            context.Vertex,
            AltitudeLayer.Building.AltitudeFor() + NativeWallMeshPlan.CompletedAltitudeOffset);
        HybridWallRayMask doorRays = context.DoorRays & rays;
        HybridWallRayMask structuralRays = rays & ~doorRays;
        bool homogeneous = allSources.All(source =>
            source.Stuff == allSources[0].Stuff &&
            source.DrawColor == allSources[0].DrawColor &&
            source.DrawColorTwo == allSources[0].DrawColorTwo);
        if ((context.DoubledRays & rays) != HybridWallRayMask.None)
        {
            Log.WarningOnce(
                "[Thin Walls] A legacy save contains two completed structures on the same shared edge. " +
                "The source-only renderer displays the canonical owner once; new designations reject this state.",
                0x54484455 ^ context.Vertex.GetHashCode());
        }

        Material canonical = CoreWallAtlasMaterial(allSources[0]);
        if (!NativeWallMeshPrinter.CanRemap(canonical))
        {
            Log.WarningOnce("[Thin Walls] Resolved wall graphic has no usable texture; displaying a diagnostic edge.",
                0x54484e53 ^ canonical.GetInstanceID());
            canonical = BaseContent.BadMat;
        }

        if (structuralRays != HybridWallRayMask.None)
        {
            IReadOnlyList<NativeWallMeshQuad> plan = NativeWallMeshPlan.Topology(structuralRays);
            if (homogeneous)
            {
                NativeWallMeshPrinter.Print(layer, center, canonical, plan);
            }
            else
            {
                const float body = 17f / 60f;
                NativeWallMeshQuad[] central = plan.Where(quad =>
                    quad.Destination.X >= -body - 0.00001f && quad.Destination.XMax <= body + 0.00001f &&
                    quad.Destination.Y >= -body - 0.00001f && quad.Destination.YMax <= body + 0.00001f).ToArray();
                NativeWallMeshPrinter.Print(layer, center, canonical, central);
                foreach (HybridWallDirection direction in context.Topology.Arms.OccupiedDirections)
                {
                    HybridWallRayMask ray = Mask(direction);
                    if (!structuralRays.HasFlag(ray) ||
                        !context.ThinSources.TryGetValue(direction, out IReadOnlyList<Building> sources)) continue;
                    NativeWallMeshQuad[] exterior = plan.Where(quad => IsExteriorForRay(quad, ray, body)).ToArray();
                    NativeWallMeshPrinter.Print(layer, center, CoreWallAtlasMaterial(sources[0]), exterior);
                }
            }
        }

        foreach (HybridWallDirection direction in context.Topology.Arms.OccupiedDirections)
        {
            HybridWallRayMask ray = Mask(direction);
            if (!doorRays.HasFlag(ray) ||
                !context.ThinSources.TryGetValue(direction, out IReadOnlyList<Building> sources)) continue;
            NativeWallMeshPrinter.Print(layer, center, CoreWallAtlasMaterial(sources[0]), NativeDoorFramePlan.ForRay(ray));
        }
    }

    private static bool IsExteriorForRay(NativeWallMeshQuad quad, HybridWallRayMask ray, float body) =>
        ray switch
        {
            HybridWallRayMask.North => quad.Destination.Y >= body - 0.00001f,
            HybridWallRayMask.East => quad.Destination.X >= body - 0.00001f,
            HybridWallRayMask.South => quad.Destination.YMax <= -body + 0.00001f,
            HybridWallRayMask.West => quad.Destination.XMax <= -body + 0.00001f,
            _ => false,
        };

    internal static bool TryPrintHybridRegularWall(
        SectionLayer layer,
        Building wall,
        Material nativeLinkedMaterial)
    {
        if (wall.Map == null || wall.def != ThingDefOf.Wall)
            return false;

        return TryPrintNativeRegularContact(layer, wall, nativeLinkedMaterial);
    }

    private static bool TryPrintNativeRegularContact(
        SectionLayer layer,
        Building wall,
        Material nativeLinked)
    {
        VertexContext[] contexts =
        {
            ResolveContext(wall.Map, wall.Position),
            ResolveContext(wall.Map, wall.Position + IntVec3.East),
            ResolveContext(wall.Map, wall.Position + IntVec3.North),
            ResolveContext(wall.Map, wall.Position + IntVec3.North + IntVec3.East),
        };
        NativeRegularContact[] contacts = contexts
            .Select(context => TryResolveNativeRegularContact(context, out NativeRegularContact contact)
                ? contact
                : null)
            .Where(contact => contact != null)
            .ToArray()!;
        if (contacts.Length != 1) return false;

        NativeRegularContact admitted = contacts[0];
        int receiverIndex = Array.FindIndex(admitted.Receivers, receiver => ReferenceEquals(receiver, wall));
        if (receiverIndex < 0) return false;
        Material atlas = CoreWallAtlasMaterial(wall);
        if (!NativeWallMeshPrinter.CanRemap(atlas) || nativeLinked == null ||
            nativeLinked.mainTexture != atlas.mainTexture || nativeLinked.shader != atlas.shader)
            return false;

        HybridWallRayMask stem = Mask(admitted.Rule.StemDirection);
        IReadOnlyList<NativeWallMeshQuad> compiled = ContactPlan(stem, receiverIndex == 0);
        Vector3 center = new(admitted.Context.Vertex.x, GenThing.TrueCenter(wall).y, admitted.Context.Vertex.z);
        NativeWallMeshPrinter.PrintLinked(
            layer,
            center,
            nativeLinked,
            wall.Position.z - admitted.Context.Vertex.z,
            compiled.Where(quad => !NativeRegularContactPlan.UsesThinSource(quad, stem)).ToArray());
        NativeWallMeshPrinter.Print(layer, center, CoreWallAtlasMaterial(admitted.Stem),
            compiled.Where(quad => NativeRegularContactPlan.UsesThinSource(quad, stem)).ToArray());
        wall.Graphic.ShadowGraphic?.Print(layer, wall, 0f);
        return true;
    }

    private static IReadOnlyList<NativeWallMeshQuad> ContactPlan(
        HybridWallRayMask stem,
        bool firstReceiver) => stem switch
    {
        HybridWallRayMask.North => NativeRegularContactPlan.NorthStem(firstReceiver),
        HybridWallRayMask.East => NativeRegularContactPlan.EastStem(firstReceiver),
        HybridWallRayMask.South => NativeRegularContactPlan.SouthStem(firstReceiver),
        HybridWallRayMask.West => NativeRegularContactPlan.WestStem(firstReceiver),
        _ => throw new ArgumentOutOfRangeException(nameof(stem)),
    };

    private static bool TryResolveNativeRegularContact(
        VertexContext context,
        out NativeRegularContact contact)
    {
        if (!TryResolveNativeRegularContactCandidate(context, out contact)) return false;
        var candidates = new List<NativeRegularContactCandidate>();
        foreach (NativeRegularGridPoint vertex in NativeRegularContactPlan.ReceiverVertices(contact.Rule))
        {
            if (TryResolveNativeRegularContactCandidate(
                    ResolveContext(context.Map, context.Vertex + new IntVec3(vertex.X, 0, vertex.Z)),
                    out NativeRegularContact candidate))
                candidates.Add(new NativeRegularContactCandidate(vertex.X, vertex.Z, candidate.Rule));
        }
        if (NativeRegularContactPlan.CanReplaceAtomically(contact.Rule, candidates)) return true;
        contact = null!;
        return false;
    }

    private static bool TryResolveNativeRegularContactCandidate(
        VertexContext context,
        out NativeRegularContact contact)
    {
        contact = null!;
        if (!NativeRegularContactPlan.TryResolve(
                context.Topology.ThinRays,
                context.Topology.OrdinaryQuadrants,
                context.DoorRays,
                context.DoubledRays,
                out NativeRegularContactRule rule))
            return false;

        if (!context.ThinSources.TryGetValue(rule.StemDirection, out IReadOnlyList<Building> sources) ||
            sources.Count != 1 || !NativeWallMeshPrinter.CanRemap(CoreWallAtlasMaterial(sources[0])))
            return false;
        IntVec3[] receiverCells =
        {
            context.Vertex + new IntVec3(rule.FirstReceiverX, 0, rule.FirstReceiverZ),
            context.Vertex + new IntVec3(rule.SecondReceiverX, 0, rule.SecondReceiverZ),
        };
        Building[] receivers = receiverCells.Select(cell => cell.GetEdifice(context.Map)).ToArray();
        foreach (Building receiver in receivers)
        {
            if (receiver == null || receiver.def != ThingDefOf.Wall ||
                !HybridRegularWallPrintPatch.UsesNativePrint(receiver.Graphic) ||
                !NativeWallMeshPrinter.CanRemap(CoreWallAtlasMaterial(receiver))) return false;
        }
        contact = new NativeRegularContact(context, rule, receivers, sources[0]);
        return true;
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
        bool regular = source.def.building?.isWall == true;
        return WallMaterial(regular ? source.Graphic : graphicDef.graphicData.Graphic, source.Stuff, source,
            source.DrawColor, source.DrawColorTwo,
            regular || graphicDef.graphicData.ignoreThingDrawColor);
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
            map,
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
        return HybridWallAtlasSupport.IsSupported(material.mainTexture);
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
            Map map,
            IntVec3 vertex,
            HybridWallVertexTopology topology,
            HybridWallRayMask doubledRays,
            HybridWallRayMask doorRays,
            IReadOnlyDictionary<HybridWallDirection, IReadOnlyList<Building>> thinSources)
        {
            Map = map;
            Vertex = vertex;
            Topology = topology;
            DoubledRays = doubledRays;
            DoorRays = doorRays;
            ThinSources = thinSources;
        }

        public Map Map { get; }
        public IntVec3 Vertex { get; }
        public HybridWallVertexTopology Topology { get; }
        public HybridWallRayMask DoubledRays { get; }
        public HybridWallRayMask DoorRays { get; }
        public IReadOnlyDictionary<HybridWallDirection, IReadOnlyList<Building>> ThinSources { get; }
    }

    private sealed class NativeRegularContact
    {
        public NativeRegularContact(
            VertexContext context,
            NativeRegularContactRule rule,
            Building[] receivers,
            Building stem)
        {
            Context = context;
            Rule = rule;
            Receivers = receivers;
            Stem = stem;
        }

        public VertexContext Context { get; }
        public NativeRegularContactRule Rule { get; }
        public Building[] Receivers { get; }
        public Building Stem { get; }
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
