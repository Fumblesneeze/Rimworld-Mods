using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest("thin-walls.building-appearance-multipart", "fumblesneeze.thinwalls",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "fumblesneeze.thinwalls",
    MaxFrames = 12000, MaxGameTicks = 1000, MaxWallClockSeconds = 180)]
public sealed class BuildingAppearanceMultipartTest : IRimWorldEndToEndTest
{
    private Building casket = null!;
    private Building neighbor = null!;
    private Pawn pawn = null!;
    private Map map = null!;
    private readonly List<Thing> zoomAnchors = new();

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap;
        BuildingAppearanceFixture.NormalizeNoonWithCleanup(context, map);
        casket = Spawn(map.Center + new IntVec3(-2, 0, 0));
        neighbor = Spawn(map.Center + new IntVec3(2, 0, 0));
        pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        GenSpawn.Spawn(pawn, map.Center + new IntVec3(0, 0, 3), map);
        var observer = new Harmony("fumblesneeze.thinwalls.e2e.appearance-mesh-observer");
        context.DeferCleanup(() =>
        {
            observer.UnpatchAll(observer.Id);
            AppearanceMeshObserver.Target = null;
            if (casket.Spawned) casket.Destroy(DestroyMode.Vanish);
            if (neighbor.Spawned) neighbor.Destroy(DestroyMode.Vanish);
            if (pawn.Spawned) pawn.Destroy(DestroyMode.Vanish);
            foreach (Thing anchor in zoomAnchors)
                if (anchor.Spawned) anchor.Destroy(DestroyMode.Vanish);
        });
        observer.Patch(AccessTools.Method(typeof(Thing), nameof(Thing.DynamicDrawPhase)),
            prefix: new HarmonyMethod(typeof(AppearanceMeshObserver), nameof(AppearanceMeshObserver.Begin)),
            finalizer: new HarmonyMethod(typeof(AppearanceMeshObserver), nameof(AppearanceMeshObserver.End)));
        foreach (MethodInfo method in AppearanceMeshObserver.SubmissionMethods())
            observer.Patch(method, prefix: new HarmonyMethod(typeof(AppearanceMeshObserver), nameof(AppearanceMeshObserver.Record))
                { priority = Priority.Last, after = new[] { ThinWallsMod.PackageId } });
        AppearanceMeshObserver.Target = casket;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause multi part comparator", true, EndToEndGameSpeed.Normal);
        yield return new CameraActionStep("frame adjusted and untouched multi part comparators", new[] { casket.ThingID, neighbor.ThingID }, 80);
        for (int direction = 0; direction < 4; direction++)
        {
            IntVec3 cell = casket.Position;
            casket.DeSpawn();
            GenSpawn.Spawn(casket, cell, map, new Rot4(direction));
            cell = neighbor.Position;
            neighbor.DeSpawn();
            GenSpawn.Spawn(neighbor, cell, map, new Rot4(direction));
            yield return new SelectionActionStep("select multipart owner " + direction, new[] { casket.ThingID }, false);
            yield return new ScreenshotStep("multipart native baseline " + direction, Array.Empty<string>(), 0);
            AppearanceMeshObserver.Submission[] baseline = AppearanceMeshObserver.Calls.ToArray();
            yield return new AssertionStep("capture body component door and ground shadow", _ =>
                EndToEndAssert.True(baseline.Length >= 3, "Expected at least body, component overlay and native shadow submissions."));
            for (int i = 0; i < 5; i++) yield return Click(context, typeof(Command_ShrinkBuilding));
            for (int i = 0; i < 3; i++) yield return Click(context, typeof(Command_OffsetBuilding));
            yield return new ScreenshotStep("multipart 50 percent east " + direction, Array.Empty<string>(), 0);
            yield return new AssertionStep("every submitted part shares the owner's transform exactly once " + direction, _ =>
            {
                var actual = AppearanceMeshObserver.Calls.ToArray();
                EndToEndAssert.Equal(baseline.Length, actual.Length, "A render part disappeared.");
                BuildingAppearance appearance = BuildingAppearanceControls.Get(casket);
                for (int part = 0; part < baseline.Length; part++)
                {
                    EndToEndAssert.Equal(baseline[part].Identity, actual[part].Identity, "Render part ordering changed.");
                    foreach (Vector3 vertex in baseline[part].Vertices)
                    {
                        Vector3 expected = appearance.Transform(baseline[part].Matrix.MultiplyPoint3x4(vertex), casket.TrueCenter());
                        Vector3 result = actual[part].Matrix.MultiplyPoint3x4(vertex);
                        EndToEndAssert.True((result - expected).sqrMagnitude < .000001f,
                            "Untransformed or double-transformed multipart draw: " + actual[part].Identity +
                            "; expected " + expected + ", actual " + result);
                    }
                }
                EndToEndAssert.True(BuildingAppearanceControls.Get(neighbor).IsDefault, "Neighbor state was changed.");
                VerifyNestedDrawRestoration(casket);
                VerifyDirectPawnRenderingIsIndependent(casket, pawn);
            });
            yield return new SelectionActionStep("unselected multipart identity " + direction, Array.Empty<string>(), false);
            yield return new ScreenshotStep("unlabeled multipart comparison " + direction, Array.Empty<string>(), 0);
            yield return Click(context, typeof(Command_ShrinkBuilding));
            for (int i = 0; i < 6; i++) yield return Click(context, typeof(Command_OffsetBuilding));
        }
        // The native closest zoom clamps small bounds, so padding alone is not a zoom test.
        // Invisible, disposable framing anchors request genuinely larger world-space bounds.
        for (int i = 0; i < 5; i++) yield return Click(context, typeof(Command_ShrinkBuilding));
        yield return new SelectionActionStep("clear selection for measured zoom comparison", Array.Empty<string>(), false);
        yield return new ScreenshotStep("closest native zoom multipart pair", Array.Empty<string>(), 0);
        float close = PixelsPerCell();
        yield return new CameraActionStep("ordinary measured multipart zoom", Frame(12), 100);
        yield return new ScreenshotStep("ordinary native zoom multipart pair", Array.Empty<string>(), 0);
        float ordinary = PixelsPerCell();
        yield return new CameraActionStep("far measured multipart zoom", Frame(18), 100);
        yield return new ScreenshotStep("far useful native zoom multipart pair", Array.Empty<string>(), 0);
        yield return new CheckpointStep("measured distinct native camera scales", _ =>
        {
            float far = PixelsPerCell();
            EndToEndAssert.True(ordinary <= close * .8f && far <= ordinary * .8f,
                "Each native zoom must reduce pixels per cell by at least 20 percent.");
            return new Dictionary<string, string>
            {
                ["closePixelsPerCell"] = close.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                ["ordinaryPixelsPerCell"] = ordinary.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                ["farPixelsPerCell"] = far.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
            };
        });
        // Same observed multipart seam, now driven by automatic adjacency rather than an Offset click.
        AppearanceMeshObserver.Target = neighbor;
        yield return new CameraActionStep("frame automatic multipart comparator", new[] { neighbor.ThingID }, 100);
        yield return new SelectionActionStep("select untouched automatic casket", new[] { neighbor.ThingID }, false);
        yield return new ScreenshotStep("multipart before native adjacent wall", Array.Empty<string>(), 0);
        var automaticBaseline = AppearanceMeshObserver.Calls.ToArray();
        CellRect footprint = neighbor.OccupiedRect();
        IntVec3 edgeCell = new(footprint.maxX, 0, footprint.minZ);
        var wallBuild = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(Array.Empty<string>(), new[] { "Structure" })
            .Single(x => x.BuildableDefName == "TW_ThinWall" && !x.Disabled);
        bool originalGod = DebugSettings.godMode;
        context.DeferCleanup(() =>
        {
            DebugSettings.godMode = originalGod;
            foreach (Thing wall in edgeCell.GetThingList(map).Where(x => x.def.defName == "TW_ThinWall").ToArray())
                wall.Destroy(DestroyMode.Vanish);
        });
        DebugSettings.godMode = true;
        yield return new GizmoActionStep("native east wall assigns automatic multipart west offset", Array.Empty<string>(),
            wallBuild.RuntimeType, wallBuild.Interaction!.Value, EndToEndCardinalRotation.East,
            new EndToEndBuildMaterial("BlocksGranite"), wallBuild.StableId,
            new EndToEndMapCell(edgeCell.x, edgeCell.z), new EndToEndMapCell(edgeCell.x, edgeCell.z), new[] { "Structure" });
        yield return new ScreenshotStep("all casket parts automatically offset west", Array.Empty<string>(), 0);
        yield return new AssertionStep("automatic multipart transform is applied exactly once", _ =>
        {
            var appearance = BuildingAppearanceControls.Get(neighbor);
            EndToEndAssert.Equal(7, appearance.OffsetStep, "East perimeter must choose west.");
            EndToEndAssert.False(appearance.OffsetIsManual, "Adjacency must not become a manual choice.");
            var actual = AppearanceMeshObserver.Calls.ToArray();
            EndToEndAssert.True(automaticBaseline.Length >= 3 && actual.Length == automaticBaseline.Length,
                "Automatic multipart capture lost body, component or shadow.");
            for (int part = 0; part < actual.Length; part++)
            {
                EndToEndAssert.Equal(automaticBaseline[part].Identity, actual[part].Identity, "Multipart inventory changed.");
                foreach (Vector3 vertex in automaticBaseline[part].Vertices)
                {
                    Vector3 expected = appearance.Transform(automaticBaseline[part].Matrix.MultiplyPoint3x4(vertex), neighbor.TrueCenter());
                    EndToEndAssert.True((actual[part].Matrix.MultiplyPoint3x4(vertex) - expected).sqrMagnitude < .000001f,
                        "Automatic offset missed or double-transformed " + actual[part].Identity);
                }
            }
        });
        foreach (var step in ThinWallNativeLogEvidence.Capture(context)) yield return step;
    }

    private IEnumerable<string> Frame(int radius)
    {
        var def = new ThingDef
        {
            defName = "TW_E2E_AppearanceZoomAnchor", label = "invisible camera anchor",
            thingClass = typeof(Thing), category = ThingCategory.Item, drawerType = DrawerType.None,
            selectable = false, useHitPoints = false, stackLimit = 1
        };
        foreach (int sign in new[] { -1, 1 })
        {
            Thing anchor = ThingMaker.MakeThing(def);
            GenSpawn.Spawn(anchor, map.Center + new IntVec3(radius * sign, 0, radius * sign), map);
            zoomAnchors.Add(anchor);
            yield return anchor.ThingID;
        }
    }

    private float PixelsPerCell()
    {
        Vector3 origin = map.Center.ToVector3();
        return Vector3.Distance(Find.Camera.WorldToScreenPoint(origin), Find.Camera.WorldToScreenPoint(origin + Vector3.right));
    }

    private Building Spawn(IntVec3 cell)
    {
        var result = (Building)ThingMaker.MakeThing(ThingDef.Named("CryptosleepCasket"));
        result.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(result, cell, map);
        return result;
    }

    private static void VerifyNestedDrawRestoration(Building owner)
    {
        BuildingAppearanceDrawScope.State outer = BuildingAppearanceDrawScope.EnterThing(owner);
        try
        {
            Matrix4x4 before = Matrix4x4.identity;
            BuildingAppearanceDrawScope.TransformSubmission(ref before);
            var separate = new ThrowingAppearanceThing { def = ThingDefOf.Steel };
            bool threw = false;
            try { separate.DrawNowAt(owner.DrawPos); }
            catch (InvalidOperationException exception) when (exception.Message == "intentional nested draw probe") { threw = true; }
            EndToEndAssert.True(threw, "Nested exception probe did not execute.");
            Matrix4x4 after = Matrix4x4.identity;
            BuildingAppearanceDrawScope.TransformSubmission(ref after);
            EndToEndAssert.Equal(before, after, "Nested exception leaked/suppressed the enclosing building transform.");
        }
        finally { BuildingAppearanceDrawScope.Restore(outer); }
    }

    private static void VerifyDirectPawnRenderingIsIndependent(Building owner, Pawn occupant)
    {
        // Supporting observation of the real native renderer used directly by vats/pods/containers.
        var baseline = AppearanceMeshObserver.Capture(() => occupant.Drawer.renderer.RenderPawnAt(occupant.DrawPos));
        BuildingAppearanceDrawScope.State outer = BuildingAppearanceDrawScope.EnterThing(owner);
        try
        {
            var nested = AppearanceMeshObserver.Capture(() => occupant.Drawer.renderer.RenderPawnAt(occupant.DrawPos));
            EndToEndAssert.True(baseline.Length > 0 && nested.Length == baseline.Length, "No native pawn submissions to compare.");
            for (int i = 0; i < baseline.Length; i++)
                EndToEndAssert.Equal(baseline[i].Matrix, nested[i].Matrix, "Directly rendered occupant inherited the building transform.");
        }
        finally { BuildingAppearanceDrawScope.Restore(outer); }
    }

    private GizmoActionStep Click(IEndToEndContext context, Type type)
    {
        var command = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { casket.ThingID }, Array.Empty<string>())
            .Single(x => x.RuntimeType == type.FullName && !x.Disabled);
        return new GizmoActionStep("native multipart " + type.Name, new[] { casket.ThingID }, command.RuntimeType,
            EndToEndGizmoInteraction.Invoke, command.StableId);
    }
}

internal sealed class ThrowingAppearanceThing : Thing
{
    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        Matrix4x4 actual = Matrix4x4.identity;
        BuildingAppearanceDrawScope.TransformSubmission(ref actual);
        EndToEndAssert.Equal(Matrix4x4.identity, actual, "An independent nested Thing inherited the building transform.");
        throw new InvalidOperationException("intentional nested draw probe");
    }
}

// Test-owned observation only: record real Unity submissions after product argument patches.
internal static class AppearanceMeshObserver
{
    public static Thing? Target;
    [ThreadStatic] private static bool observing;
    private static int frame = -1;
    public static readonly List<Submission> Calls = new();
    public static void Begin(Thing __instance, out bool __state)
    { __state = observing; observing = ReferenceEquals(__instance, Target); }
    public static Exception? End(Exception? __exception, bool __state)
    { observing = __state; return __exception; }
    public static void Record(Mesh mesh, Matrix4x4 matrix, Material material)
    {
        if (!observing) return;
        if (frame != Time.frameCount) { frame = Time.frameCount; Calls.Clear(); }
        if (Calls.Count >= 32) throw new EndToEndAssertionException("Unbounded multipart diagnostic capture.");
        string textureName = material.HasProperty("_MainTex") ? material.mainTexture?.name ?? "none" : "untextured";
        Calls.Add(new Submission(material.name + "/" + textureName, matrix, mesh.vertices));
    }
    public static Submission[] Capture(Action draw)
    {
        bool previous = observing;
        var previousCalls = Calls.ToArray();
        int previousFrame = frame;
        try
        {
            observing = true;
            frame = Time.frameCount;
            Calls.Clear();
            draw();
            return Calls.ToArray();
        }
        finally
        {
            observing = previous;
            frame = previousFrame;
            Calls.Clear();
            Calls.AddRange(previousCalls);
        }
    }
    public readonly struct Submission
    {
        public readonly string Identity;
        public readonly Matrix4x4 Matrix;
        public readonly Vector3[] Vertices;
        public Submission(string identity, Matrix4x4 matrix, Vector3[] vertices)
        { Identity = identity; Matrix = matrix; Vertices = vertices; }
    }
    public static IEnumerable<MethodInfo> SubmissionMethods()
    {
        var signature = new[] { typeof(Mesh), typeof(Matrix4x4), typeof(Material), typeof(int), typeof(Camera), typeof(int),
            typeof(MaterialPropertyBlock), typeof(ShadowCastingMode), typeof(bool), typeof(Transform), typeof(LightProbeUsage),
            typeof(LightProbeProxyVolume) };
        yield return AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh), signature);
        yield return AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh), signature.Take(11).ToArray());
    }
}
