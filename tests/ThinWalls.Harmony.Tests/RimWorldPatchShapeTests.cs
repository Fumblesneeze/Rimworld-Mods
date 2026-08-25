using System.Reflection;
using Mono.Cecil;
using NUnit.Framework;
using ThinWalls.Geometry;

namespace ThinWalls.Harmony.Tests;

[TestFixture]
public sealed class RimWorldPatchShapeTests
{
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
            Assert.That(product.MainModule.GetType("ThinWalls.Rendering.HybridRegularWallPrintPatch"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Rendering.AdjacentBuildingDrawPosPatch"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Rendering.AdjacentBuildingMapMeshPrintPatch"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Rendering.AdjacentBuildingMapMeshCenterPatch"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Rendering.ThinEdgePhaseSpawnPatch"), Is.Not.Null);
            Assert.That(product.MainModule.GetType("ThinWalls.Rendering.ThinEdgePhaseDeSpawnPatch"), Is.Not.Null);
        });
    }

    [Test]
    public void AdjacentBuildingVisualRevisionChangesOnlyForThinEdgeLifecycle()
    {
        using AssemblyDefinition product = AssemblyDefinition.ReadAssembly(typeof(ThinWallSide).Assembly.Location);
        TypeDefinition? component = product.MainModule.GetType("ThinWalls.Pathing.ThinWallMapComponent");
        MethodDefinition? increment = component?.Methods.SingleOrDefault(method =>
            method.Name == "IncrementThinEdgeVisualRevision");

        Assert.That(component, Is.Not.Null, "Missing ThinWallMapComponent.");
        Assert.That(increment, Is.Not.Null, "Missing the dedicated Thin-edge visual revision seam.");

        string[] callers = component!.Methods
            .Where(method => method.HasBody && method.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference called &&
                called.Resolve() == increment))
            .Select(method => method.Name)
            .OrderBy(name => name, System.StringComparer.Ordinal)
            .ToArray();

        Assert.That(callers, Is.EqualTo(new[]
        {
            "NotifyCompletedEdgeChanged",
            "NotifyPlannedEdgeChanged",
        }), "ordinary buildings, doors, terrain, and path-cost events must not invalidate the visual perimeter cache");
    }

    [Test]
    public void OrdinaryBuildingBlueprintAndFrameUseThePatchedBaseDrawPositionGetter()
    {
        using AssemblyDefinition game = AssemblyDefinition.ReadAssembly(Metadata("AssemblyCSharpPath"));
        foreach (string typeName in new[]
                 {
                     "Verse.Building",
                     "RimWorld.Blueprint",
                     "RimWorld.Frame",
                 })
        {
            TypeDefinition? type = game.MainModule.GetType(typeName);
            Assert.That(type, Is.Not.Null, $"Missing {typeName}.");
            Assert.That(
                type!.Methods.Any(method => method.Name == "get_DrawPos"),
                Is.False,
                $"{typeName} overrides DrawPos, so the narrow Thing getter patch would not cover its render phase.");
        }
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
                called.DeclaringType.FullName == "ThinWalls.Rendering.CoreDerivedWallMaterialCache" &&
                called.Name == "RealtimeMaterial"),
            Is.True,
            "a Custom/Cutout dynamic leaf is hidden by the cached map mesh; the leaf must use the realtime transparent queue");
    }

    [Test]
    public void RealtimeDoorQueuePreservesTheMaskCapableCoreShader()
    {
        using AssemblyDefinition product = AssemblyDefinition.ReadAssembly(typeof(ThinWallSide).Assembly.Location);
        TypeDefinition? cache = product.MainModule.GetType("ThinWalls.Rendering.CoreDerivedWallMaterialCache");
        MethodDefinition? realtime = cache?.Methods.SingleOrDefault(method => method.Name == "RealtimeMaterial");

        Assert.That(realtime, Is.Not.Null, "Missing CoreDerivedWallMaterialCache.RealtimeMaterial.");
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
