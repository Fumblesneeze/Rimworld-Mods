using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace ThinWalls.Defs.Tests;

[TestFixture]
public sealed class ThinWallPackageContractTests
{
    private static readonly string[] ExactRuntimeFiles =
    {
        "1.6/Assemblies/ThinWalls.dll",
        "1.6/Assemblies/ThinWalls.pdb",
        "1.6/Defs/JobDefs/ThinWallJobs.xml",
        "1.6/Defs/MiscDefs/ThinWallDrawStyle.xml",
        "1.6/Defs/ThingDefs/ThinWalls.xml",
        "1.6/Languages/English/DefInjected/JobDef/ThinWallJobs.xml",
        "1.6/Languages/English/DefInjected/ThingDef/ThinWall.xml",
        "1.6/Languages/English/Keyed/ThinWalls.xml",
        "1.6/Languages/German/DefInjected/JobDef/ThinWallJobs.xml",
        "1.6/Languages/German/DefInjected/ThingDef/ThinWall.xml",
        "1.6/Languages/German/Keyed/ThinWalls.xml",
        "1.6/Patches/StructureDesignator.xml",
        "About/About.xml",
        "About/Preview.png",
    };

    [Test]
    public void ReleasePackageContainsCodeAndDefsButNoStructuralRasterOrGatewayDependency()
    {
        string root = RepositoryRoot();
        string package = Path.Combine(root, "artifacts", "Mods", "fumblesneeze.thinwalls");
        string[] actual = Directory.GetFiles(package, "*", SearchOption.AllDirectories)
            .Select(path => path.Substring(package.Length + 1).Replace(Path.DirectorySeparatorChar, '/'))
            .OrderBy(path => path)
            .ToArray();
        string assemblyPath = Path.Combine(package, "1.6", "Assemblies", "ThinWalls.dll");
        string[] references = Assembly.ReflectionOnlyLoadFrom(assemblyPath)
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(ExactRuntimeFiles.OrderBy(path => path).ToArray()));
            Assert.That(actual.Where(path => path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)).ToArray(),
                Is.EqualTo(new[] { "About/Preview.png" }));
            Assert.That(actual.Any(path => path.IndexOf("Test", System.StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
            Assert.That(actual.Any(path => path.IndexOf("Gateway", System.StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
            Assert.That(references.Any(reference => reference.StartsWith("RimWorldDevGateway")), Is.False);
            Assert.That(references, Does.Not.Contain("nunit.framework"));
        });
    }

    private static string RepositoryRoot() => typeof(ThinWallPackageContractTests).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(attribute => attribute.Key == "RepositoryRoot")
        .Value;
}
