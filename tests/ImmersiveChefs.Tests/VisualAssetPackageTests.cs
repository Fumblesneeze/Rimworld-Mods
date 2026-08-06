using System.Drawing;
using System.IO;
using System.Xml.Linq;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class VisualAssetPackageTests
{
    private const string CookwareTexturePath =
        "ImmersiveChefs/Things/Item/Kitchenware/Cookware/Cookware";

    [Test]
    public void Ordinary_cookware_uses_owned_stuffable_art_with_a_matching_mask()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "Kitchenware.xml"));
        var def = document.Root!.Elements("ThingDef")
            .Single(element =>
                (string?)element.Element("defName") == "ImmersiveChefs_Cookware");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, CookwareTexturePath + ".png");
        var maskPath = TextureFile(root, CookwareTexturePath + "_m.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(CookwareTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("CutoutComplex"));
            Assert.That(File.Exists(diffusePath), Is.True, "The selected cookware sprite must be packaged.");
            Assert.That(File.Exists(maskPath), Is.True, "Stuffable cookware needs a matching RimWorld mask.");
        });

        if (!File.Exists(diffusePath) || !File.Exists(maskPath))
        {
            return;
        }

        using var diffuse = new Bitmap(diffusePath);
        using var mask = new Bitmap(maskPath);
        Assert.Multiple(() =>
        {
            Assert.That(diffuse.Size, Is.EqualTo(mask.Size));
            Assert.That(diffuse.Width, Is.GreaterThanOrEqualTo(128));
            Assert.That(diffuse.Height, Is.GreaterThanOrEqualTo(128));
            Assert.That(diffuse.GetPixel(0, 0).A, Is.EqualTo(0));
            Assert.That(diffuse.GetPixel(diffuse.Width - 1, 0).A, Is.EqualTo(0));
            Assert.That(diffuse.GetPixel(0, diffuse.Height - 1).A, Is.EqualTo(0));
            Assert.That(diffuse.GetPixel(diffuse.Width - 1, diffuse.Height - 1).A, Is.EqualTo(0));
            Assert.That(mask.GetPixel(0, 0).A, Is.EqualTo(0));
        });
    }

    private static string TextureFile(string root, string texturePath)
    {
        return Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Textures",
            texturePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ImmersiveChefs.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Immersive Chefs repository root.");
    }
}
