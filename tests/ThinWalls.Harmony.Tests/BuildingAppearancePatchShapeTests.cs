using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using NUnit.Framework;

namespace ThinWalls.Harmony.Tests;

[TestFixture]
public sealed class BuildingAppearancePatchShapeTests
{
    [Test]
    public void ClaimRefreshRunsAfterTheNativeFactionChange()
    {
        using var product = AssemblyDefinition.ReadAssembly(typeof(ThinWalls.Rendering.BuildingAppearance).Assembly.Location);
        TypeDefinition patch = product.MainModule.GetType("ThinWalls.Rendering.BuildingAppearanceFactionPatch");
        Assert.That(patch, Is.Not.Null, "Claim does not emit a BuildingSpawned event.");
        MethodDefinition postfix = patch.Methods.Single(method => method.Name == "Postfix");
        Assert.That(postfix.CustomAttributes.Any(attribute => attribute.AttributeType.Name == "HarmonyPostfix"), Is.True);
        Assert.That(postfix.Body.Instructions.Any(instruction => instruction.Operand is MethodReference called &&
            called.DeclaringType.FullName == "ThinWalls.Rendering.BuildingAppearanceControls" &&
            called.Name == "RefreshAutomaticOffset"), Is.True);
    }

    [Test]
    public void OnlyTheUnifiedAppearancePipelineOwnsAutomaticBuildingOffsets()
    {
        using var product = AssemblyDefinition.ReadAssembly(typeof(ThinWalls.Rendering.BuildingAppearance).Assembly.Location);
        foreach (string type in new[] { "AdjacentBuildingDrawPosPatch", "AdjacentBuildingMapMeshPrintPatch",
                     "AdjacentBuildingMapMeshCenterPatch", "AdjacentBuildingMapMeshContext", "AdjacentBuildingGraphicClearance",
                     "AdjacentBuildingVisualOffset" })
            Assert.That(product.MainModule.GetType("ThinWalls.Rendering." + type), Is.Null, type);
        Assert.That(product.MainModule.GetType("ThinWalls.Rendering.BuildingAppearancePrintPatch"), Is.Not.Null);
        Assert.That(product.MainModule.GetType("ThinWalls.Rendering.BuildingAppearanceMeshSubmissionPatch"), Is.Not.Null);
    }

    [Test]
    public void UnityDrawMeshOverloadsHaveExactlyTwoIndependentManagedSubmissionSinks()
    {
        string game = typeof(BuildingAppearancePatchShapeTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(x => x.Key == "AssemblyCSharpPath").Value;
        using var unity = AssemblyDefinition.ReadAssembly(Path.Combine(Path.GetDirectoryName(game)!, "UnityEngine.CoreModule.dll"));
        var overloads = unity.MainModule.GetType("UnityEngine.Graphics").Methods.Where(x => x.Name == "DrawMesh").ToArray();
        var sinks = overloads.Where(x => x.HasBody && x.Body.Instructions.Any(i =>
            i.Operand is MethodReference method && method.Name == "Internal_DrawMesh")).ToArray();
        Assert.That(sinks.Select(x => x.Parameters.Count), Is.EquivalentTo(new[] { 11, 12 }));
        foreach (var sink in sinks)
        {
            Assert.That(sink.IsStatic && sink.IsPublic && sink.ReturnType.FullName == "System.Void", Is.True);
            Assert.That(sink.Parameters[1].ParameterType.FullName, Is.EqualTo("UnityEngine.Matrix4x4"));
            Assert.That(sink.Body.Instructions.Any(i => i.Operand is MethodReference method && method.Name == "DrawMesh"), Is.False,
                "Terminal submissions must not call each other, or one mesh would be transformed twice.");
        }
        foreach (var wrapper in overloads.Except(sinks))
            Assert.That(wrapper.Body.Instructions.Any(i => i.Operand is MethodReference method &&
                sinks.Any(sink => sink.FullName == method.FullName)), Is.True, wrapper.FullName);
    }
}
