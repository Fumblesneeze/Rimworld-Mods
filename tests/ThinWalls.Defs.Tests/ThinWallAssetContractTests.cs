using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace ThinWalls.Defs.Tests;

[TestFixture]
public sealed class ThinWallAssetContractTests
{
    [Test]
    public void RuntimePackageHasNoStructuralRasterOrRejectedBitmapManifest()
    {
        string mod = ModRoot();

        Assert.Multiple(() =>
        {
            string textures = Path.Combine(mod, "Textures");
            Assert.That(Directory.Exists(textures)
                ? Directory.GetFiles(textures, "*", SearchOption.AllDirectories)
                : System.Array.Empty<string>(), Is.Empty);
            Assert.That(File.Exists(Path.Combine(mod, "Assets", "ThinWallVisualManifest.json")), Is.False);
            Assert.That(Directory.GetFiles(mod, "*.png", SearchOption.AllDirectories)
                    .Where(path => path.IndexOf(Path.Combine("Release", "workshop"), System.StringComparison.OrdinalIgnoreCase) < 0)
                    .Select(path => path.Substring(mod.Length + 1).Replace(Path.DirectorySeparatorChar, '/'))
                    .ToArray(),
                Is.EqualTo(new[] { "About/Preview.png" }),
                "the only runtime PNG is the accepted in-game-derived About preview");
        });
    }

    [Test]
    public void DefsReferenceInstalledCoreWallPixelsInsteadOfPackagedThinWallArt()
    {
        string xml = File.ReadAllText(Path.Combine(ModRoot(), "Defs", "ThingDefs", "ThinWalls.xml"));

        Assert.Multiple(() =>
        {
            Assert.That(xml, Does.Contain("Things/Building/Linked/Wall"));
            Assert.That(xml, Does.Not.Contain("Things/Building/Linked/Wall_Bricks"));
            Assert.That(xml, Does.Contain("<graphicClass>Graphic_Single</graphicClass>"));
            Assert.That(xml, Does.Not.Contain("<graphicClass>Graphic_Appearances</graphicClass>"),
                "the fallback Def graphic is a native menu icon; the map renderer resolves Core's atlas independently, so native icon resolution must not expose it");
            Assert.That(xml, Does.Contain("Things/Building/Linked/WallSmooth_MenuIcon"));
            Assert.That(xml, Does.Contain("Things/Building/Door/DoorSimple_MenuIcon"));
            Assert.That(xml, Does.Contain("Things/Building/Linked/WallBricks_MenuIcon"));
            Assert.That(xml, Does.Contain("Things/Building/Linked/WallPlanks_MenuIcon"));
            Assert.That(xml, Does.Not.Contain("ThinWalls/Wall/"));
        });
    }

    [Test]
    public void NoThinWallImageGenerationRecipeOrLegacyComponentNameRemains()
    {
        string root = RepositoryRoot();
        string recipeDirectory = Path.Combine(root, "scripts", "AssetGeneration", "ThinWalls");
        string source = string.Join("\n", Directory.GetFiles(Path.Combine(ModRoot(), "Source"), "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText));

        Assert.Multiple(() =>
        {
            Assert.That(Directory.Exists(recipeDirectory) && Directory.GetFiles(recipeDirectory).Length > 0, Is.False);
            Assert.That(source, Does.Not.Contain("RegularJoin"));
            Assert.That(source, Does.Not.Contain("StraightJoin"));
            Assert.That(source, Does.Not.Contain("DoorJamb.png"));
            Assert.That(source, Does.Not.Contain("Post.png"));
            Assert.That(source, Does.Not.Contain("DamageTexturePath"));
        });
    }

    [Test]
    public void RuntimeDoesNotReadBackOrHashTexturePixelsForContentAdmission()
    {
        string source = File.ReadAllText(Path.Combine(
            ModRoot(),
            "Source",
            "Rendering",
            "HybridWallAtlasSupport.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Not.Contain("ComputeRgbaSha256"));
            Assert.That(source, Does.Not.Contain("GetPixels32"));
            Assert.That(source, Does.Not.Contain("Wall_Atlas_Bricks"));
        });
    }

    private static string ModRoot() => Path.Combine(RepositoryRoot(), "mods", "ThinWalls");

    private static string RepositoryRoot() => typeof(ThinWallAssetContractTests).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(attribute => attribute.Key == "RepositoryRoot")
        .Value;
}
