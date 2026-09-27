using System.Reflection;
using Mono.Cecil;
using NUnit.Framework;
using ThinWalls.Geometry;

namespace ThinWalls.Harmony.Tests;

[TestFixture]
public sealed class RimWorldPatchShapeTests
{
    [Test]
    public void ThinPathingOwnsNoPersistentMapGridOrOrdinaryPathCostSubscription()
    {
        using AssemblyDefinition product = AssemblyDefinition.ReadAssembly(typeof(ThinWallSide).Assembly.Location);
        TypeDefinition component = product.MainModule.GetType("ThinWalls.Pathing.ThinWallMapComponent");
        Assert.That(component.Fields.Where(f => f.FieldType.FullName.StartsWith("Unity.Collections.NativeArray`1<")), Is.Empty,
            "Persistent full-map connectivity mirrors must be replaced by sparse owned-edge masks.");
        Assert.That(component.Methods.SelectMany(m => m.HasBody ? m.Body.Instructions : Enumerable.Empty<Mono.Cecil.Cil.Instruction>())
            .Any(i => i.Operand is MethodReference call && call.Name == "add_PathCostRecalculate"), Is.False,
            "Ordinary movement/reservation changes must not invalidate Thin-owned reachability.");
    }

    [Test]
    public void NativeConnectivityGatherRunsOnlyAfterScheduledReadersComplete()
    {
        using AssemblyDefinition game = AssemblyDefinition.ReadAssembly(Metadata("AssemblyCSharpPath"));
        var finder = game.MainModule.GetType("Verse.PathFinder");
        int callers = 0;
        foreach (var method in finder.Methods.Where(m => m.HasBody))
        {
            var calls = method.Body.Instructions.Where(i => i.Operand is MethodReference).Select(i => (MethodReference)i.Operand).ToList();
            int gather = calls.FindIndex(m => m.DeclaringType.FullName == "Verse.PathFinderMapData" && m.Name == "GatherData");
            if (gather < 0) continue;
            callers++;
            Assert.That(calls.Take(gather).Any(m => m.Name == "ForceCompleteScheduledJobs"), Is.True, method.FullName);
        }
        Assert.That(callers, Is.EqualTo(2), "Both asynchronous tick and synchronous path calls must be inspected.");
        var source = game.MainModule.GetType("Verse.SimplePathFinderDataSource`1");
        Assert.That(source.Fields.Single(f => f.Name == "data").FieldType.FullName, Does.StartWith("Unity.Collections.NativeArray`1<"));
    }

    [Test]
    public void CellFloodTranspilerKeepsTheNativeFloodAndOnlyAddsItsEdgePredicate()
    {
        var original = HarmonyLib.AccessTools.Method(typeof(Verse.Reachability), "CheckCellBasedReachability");
        var native = HarmonyLib.PatchProcessor.GetOriginalInstructions(original);
        var patched = ThinWalls.Pathing.ThinEdgeCellFloodPatch.Transpiler(native).ToArray();
        Assert.That(patched.Count(i => i.operand is MethodInfo m && m.DeclaringType == typeof(ThinWalls.Pathing.ThinEdgeCellFloodPatch) && m.Name == "Flood"), Is.EqualTo(1));
        Assert.That(patched.Any(i => i.operand is MethodInfo m && m.DeclaringType == typeof(Verse.FloodFiller) && m.Name == "FloodFill"), Is.False);
    }

    [Test]
    public void NativeTouchDestinationCollectorHasAnOwnedLocalEdgeFilter()
    {
        using AssemblyDefinition game = AssemblyDefinition.ReadAssembly(Metadata("AssemblyCSharpPath"));
        AssertMethod(game, "Verse.AI.TouchPathEndModeUtility", "AddAllowedAdjacentRegions", 4);
        using AssemblyDefinition product = AssemblyDefinition.ReadAssembly(typeof(ThinWallSide).Assembly.Location);
        Assert.That(product.MainModule.GetType("ThinWalls.Pathing.ThinWallTouchRegionsPatch"), Is.Not.Null,
            "Native region Touch destination collection must not introduce destinations across a thin edge.");
    }

    [TestCase("Blueprint_ThinWall")]
    [TestCase("Blueprint_ThinDoor")]
    public void EdgeBlueprintReplacesTheInheritedCellAtlasPrint(string name)
    {
        using AssemblyDefinition product = AssemblyDefinition.ReadAssembly(typeof(ThinWallSide).Assembly.Location);
        TypeDefinition blueprint = product.MainModule.GetType("ThinWalls.Buildings." + name);
        Assert.That(blueprint.Methods.Any(method => method.Name == "Print" && method.IsVirtual), Is.True,
            "MapMeshAndRealTime blueprints must own Print as well as DrawAt, or the inherited full atlas leaks.");
    }

    [Test]
    public void SelectionUsesNativeBracketCalculationWithAnEdgeEnvelopePatch()
    {
        using AssemblyDefinition game = AssemblyDefinition.ReadAssembly(Metadata("AssemblyCSharpPath"));
        AssertMethod(game, "RimWorld.SelectionDrawerUtility", "CalculateSelectionBracketPositionsWorld", 8);
        using AssemblyDefinition product = AssemblyDefinition.ReadAssembly(typeof(ThinWallSide).Assembly.Location);
        Assert.That(product.MainModule.GetType("ThinWalls.Rendering.ThinWallSelectionPatch"), Is.Not.Null);
    }

    [Test]
    public void RimWorld16PathPlacementReachabilityAndMovementSeamsExist()
    {
        using AssemblyDefinition game = AssemblyDefinition.ReadAssembly(Metadata("AssemblyCSharpPath"));

        Assert.Multiple(() =>
        {
            AssertMethod(game, "Verse.PathFinderMapData", "GatherData", 1);
            AssertMethod(game, "Verse.PathFinderMapData", "ParameterizePathJob", 1);
            AssertMethod(game, "Verse.PathFinder", "ParameterizePathJob", 7);
            AssertMethod(game, "Verse.PathFinder", "Dispose", 0);
            AssertMethod(game, "RimWorld.GenConstruct", "CanPlaceBlueprintAt_NewTemp", 12);
            AssertMethod(game, "Verse.Reachability", "CanReach", 4);
            AssertMethod(game, "Verse.ReachabilityImmediate", "CanReachImmediate", 5);
            AssertMethod(game, "Verse.AI.Pawn_PathFollower", "SetupMoveIntoNextCell", 0);
            AssertMethod(game, "Verse.AI.Pawn_PathFollower", "TryEnterNextPathCell", 0);
            AssertMethod(game, "Verse.AI.Pawn_PathFollower", "NextCellDoorToWaitForOrManuallyOpen", 0);
            AssertMethod(game, "Verse.PathFinder", "EnsureDoorsPawnsCached", 0);
            AssertMethod(game, "Verse.PathFinder", "ForceCompleteScheduledJobs", 0);
            AssertMethod(game, "Verse.RegionMaker", "TryGenerateRegionFrom", 1);
            AssertMethod(game, "RimWorld.Building_Door", "get_BlockedOpenMomentary", 0);
            AssertMethod(game, "RimWorld.Building_Door", "CheckFriendlyTouched", 1);
            AssertMethod(game, "Verse.GenTemperature", "EqualizeTemperaturesThroughBuilding", 3);
            AssertMethod(game, "Verse.Graphic_Linked", "Print", 3);
            AssertMethod(game, "Verse.Thing", "get_DrawPos", 0);
            AssertMethod(game, "Verse.Thing", "SpawnSetup", 2);
            AssertMethod(game, "Verse.Thing", "DeSpawn", 1);
            AssertMethod(game, "RimWorld.Ideo", "MembersCanBuild", 1);
        });
    }

    [Test]
    public void ProductDeclaresNarrowPatchOwnersForEveryRequiredSeam()
    {
        using AssemblyDefinition product = AssemblyDefinition.ReadAssembly(typeof(ThinWallSide).Assembly.Location);

        Assert.Multiple(() =>
        {
            Assert.That(product.MainModule.GetType("ThinWalls.Pathing.PathFinderMapDataPatches"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Placement.ThinWallPlacementPatch"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Placement.ThinWallSpawningWipesPatch"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Pathing.ThinWallReachabilityPatch"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Pathing.ThinWallReachabilityImmediatePatch"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Pathing.ThinWallMovementPatches"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Pathing.JobDriver_AttackThinWallEdge"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Construction.ThinWallIdeologyPatch"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Pathing.ThinDoorMovementPatches"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Pathing.ThinDoorPathFinderPatches"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Rooms.ThinEdgeRegionMakerPatch"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Rooms.ThinDoorTemperaturePatch"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Rendering.HybridRegularWallPrintPatch"), Is.Not.Null,
                "The bounded two-receiver diagonal shoulder must replace only its two receiver prints.");
            Assert.That(product.MainModule.GetType("ThinWalls.Rendering.BuildingAppearancePrintPatch"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Rendering.BuildingAppearanceMeshSubmissionPatch"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Rendering.ThinEdgePhaseSpawnPatch"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Rendering.ThinEdgePhaseDeSpawnPatch"), Is.Not.Null);
        });
    }

    [Test]
    public void AutomaticAppearanceRefreshUsesLocalBuildingAndThinEdgeLifecycle()
    {
        using AssemblyDefinition product = AssemblyDefinition.ReadAssembly(typeof(ThinWallSide).Assembly.Location);
        TypeDefinition? component = product.MainModule.GetType("ThinWalls.Pathing.ThinWallMapComponent");
        Assert.That(component, Is.Not.Null, "Missing ThinWallMapComponent.");
        Assert.That(component!.Properties.Any(property => property.Name == "ThinEdgeVisualRevision"), Is.False,
            "The obsolete render-time perimeter cache no longer needs a global visual revision.");

        string[] callers = component!.Methods
            .Where(method => method.HasBody && method.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference called &&
                called.DeclaringType.FullName == "ThinWalls.Rendering.BuildingAppearanceControls" &&
                called.Name == "RefreshAutomaticOffset"))
            .Select(method => method.Name)
            .OrderBy(name => name, System.StringComparer.Ordinal)
            .ToArray();

        Assert.That(callers, Is.EqualTo(new[]
        {
            "DirtyIncidentThingMeshes",
            "NotifyBuildingChanged",
        }), "automatic choices are assigned locally, never from the render/tick path");
    }

    [Test]
    public void ThinDoorDrawDoesNotCallVanillaDoorPreDrawThatReorientsOneCellDoors()
    {
        using AssemblyDefinition product = AssemblyDefinition.ReadAssembly(typeof(ThinWallSide).Assembly.Location);
        TypeDefinition? thinDoor = product.MainModule.GetType("ThinWalls.Buildings.Building_ThinDoor");
        MethodDefinition? drawAt = thinDoor?.Methods.SingleOrDefault(method => method.Name == "DrawAt");

        Assert.That(thinDoor, Is.Not.Null, "Missing Building_ThinDoor.");
        Assert.That(drawAt, Is.Not.Null, "Missing Building_ThinDoor.DrawAt override.");
        Assert.That(
            drawAt!.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference called && called.Name == "DoorPreDraw"),
            Is.False,
            "Vanilla DoorPreDraw mutates Rotation from neighboring full-cell walls; a Thin Door must preserve its owned edge while drawing.");
    }

    [Test]
    public void ThinDoorDynamicLeafUsesTheRealtimeMaterialQueue()
    {
        using AssemblyDefinition product = AssemblyDefinition.ReadAssembly(typeof(ThinWallSide).Assembly.Location);
        TypeDefinition? renderer = product.MainModule.GetType("ThinWalls.Rendering.HybridWallRenderer");
        MethodDefinition? drawDoor = renderer?.Methods.SingleOrDefault(method => method.Name == "DrawDoor");

        Assert.That(drawDoor, Is.Not.Null, "Missing HybridWallRenderer.DrawDoor.");
        Assert.That(drawDoor!.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference called &&
                called.DeclaringType.FullName == "ThinWalls.Rendering.HybridWallRenderer" &&
                called.Name == "RealtimeMaterial"),
            Is.True,
            "a Custom/Cutout dynamic leaf is hidden by the cached map mesh; the leaf must use the realtime transparent queue");
    }

    [Test]
    public void RealtimeDoorQueuePreservesTheMaskCapableCoreShader()
    {
        using AssemblyDefinition product = AssemblyDefinition.ReadAssembly(typeof(ThinWallSide).Assembly.Location);
        TypeDefinition? renderer = product.MainModule.GetType("ThinWalls.Rendering.HybridWallRenderer");
        MethodDefinition? realtime = renderer?.Methods.SingleOrDefault(method => method.Name == "RealtimeMaterial");

        Assert.That(realtime, Is.Not.Null, "Missing HybridWallRenderer.RealtimeMaterial.");
        Assert.Multiple(() =>
        {
            Assert.That(realtime!.Body.Instructions.Any(instruction =>
                    instruction.Operand is MethodReference called && called.Name == "set_renderQueue"),
                Is.True,
                "the realtime leaf must move later by queue override while retaining CutoutComplex mask sampling");
            Assert.That(realtime.Body.Instructions.Any(instruction =>
                    instruction.Operand is MethodReference called && called.Name == "set_shader"),
                Is.False,
                "replacing CutoutComplex with Transparent drops the inherited Stuff mask channels");
        });
    }

    private static void AssertMethod(AssemblyDefinition assembly, string typeName, string methodName, int parameters)
    {
        TypeDefinition? type = assembly.MainModule.GetType(typeName);
        Assert.That(type, Is.Not.Null, $"Missing type {typeName}");
        Assert.That(type!.Methods.Any(method => method.Name == methodName && method.Parameters.Count == parameters),
            Is.True, $"Missing {typeName}.{methodName}/{parameters}");
    }

    private static string Metadata(string key)
    {
        return typeof(RimWorldPatchShapeTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == key)
            .Value;
    }
}
