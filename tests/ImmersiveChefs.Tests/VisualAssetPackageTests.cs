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
    private const string PlateTexturePath =
        "ImmersiveChefs/Things/Item/Kitchenware/Plate/Plate";
    private const string CutleryTexturePath =
        "ImmersiveChefs/Things/Item/Kitchenware/Cutlery/Cutlery";

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
            Assert.That(File.Exists(diffusePath), Is.True, "The selected cookware source sprite must exist.");
            Assert.That(File.Exists(maskPath), Is.True, "Stuffable cookware needs a source RimWorld mask.");
            Assert.That(File.Exists(PackagedTextureFile(root, CookwareTexturePath + ".png")), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, CookwareTexturePath + "_m.png")), Is.True);
        });

        AssertTransparentMatchingPair(diffusePath, maskPath, requireFixedBlackRegion: true);
    }

    [Test]
    public void Plate_family_uses_owned_single_sprite_art_with_a_stuff_mask()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "Kitchenware.xml"));
        var defs = document.Root!.Elements("ThingDef")
            .Where(element => element.Element("defName") is not null)
            .ToDictionary(element => (string)element.Element("defName")!);
        var plateGraphic = defs["ImmersiveChefs_Plate"].Element("graphicData")!;
        var adobeGraphic = defs["ImmersiveChefs_AdobePlate"].Element("graphicData")!;
        var diffusePath = TextureFile(root, PlateTexturePath + ".png");
        var maskPath = TextureFile(root, PlateTexturePath + "_m.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)plateGraphic.Element("texPath"), Is.EqualTo(PlateTexturePath));
            Assert.That((string?)plateGraphic.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)plateGraphic.Element("shaderType"), Is.EqualTo("CutoutComplex"));
            Assert.That((string?)adobeGraphic.Element("texPath"), Is.EqualTo(PlateTexturePath));
            Assert.That((string?)adobeGraphic.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(maskPath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, PlateTexturePath + ".png")), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, PlateTexturePath + "_m.png")), Is.True);
        });

        AssertTransparentMatchingPair(diffusePath, maskPath, requireFixedBlackRegion: false);
    }

    [Test]
    public void Cutlery_setting_uses_owned_single_sprite_art_with_a_stuff_mask()
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
                (string?)element.Element("defName") == "ImmersiveChefs_Cutlery");
        var graphicData = def.Element("graphicData")!;
        var diffusePath = TextureFile(root, CutleryTexturePath + ".png");
        var maskPath = TextureFile(root, CutleryTexturePath + "_m.png");

        Assert.Multiple(() =>
        {
            Assert.That((string?)graphicData.Element("texPath"), Is.EqualTo(CutleryTexturePath));
            Assert.That((string?)graphicData.Element("graphicClass"), Is.EqualTo("Graphic_Single"));
            Assert.That((string?)graphicData.Element("shaderType"), Is.EqualTo("CutoutComplex"));
            Assert.That(File.Exists(diffusePath), Is.True);
            Assert.That(File.Exists(maskPath), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, CutleryTexturePath + ".png")), Is.True);
            Assert.That(File.Exists(PackagedTextureFile(root, CutleryTexturePath + "_m.png")), Is.True);
        });

        AssertTransparentMatchingPair(diffusePath, maskPath, requireFixedBlackRegion: false);
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

    private static string PackagedTextureFile(string root, string texturePath)
    {
        return Path.Combine(
            root,
            "artifacts",
            "Mods",
            "fumblesneeze.immersivechefs",
            "1.6",
            "Textures",
            texturePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void AssertTransparentMatchingPair(
        string diffusePath,
        string maskPath,
        bool requireFixedBlackRegion)
    {
        if (!File.Exists(diffusePath) || !File.Exists(maskPath))
        {
            return;
        }

        using var diffuse = new Bitmap(diffusePath);
        using var mask = new Bitmap(maskPath);
        var visibleDiffusePixels = 0;
        var redStuffPixels = 0;
        var fixedBlackPixels = 0;
        var alphaMismatches = 0;
        for (var y = 0; y < diffuse.Height; y++)
        {
            for (var x = 0; x < diffuse.Width; x++)
            {
                var diffusePixel = diffuse.GetPixel(x, y);
                var maskPixel = mask.GetPixel(x, y);
                if (diffusePixel.A > 0)
                {
                    visibleDiffusePixels++;
                }

                if (maskPixel.A > 0 && maskPixel.R >= 240 && maskPixel.G <= 15 && maskPixel.B <= 15)
                {
                    redStuffPixels++;
                }

                if (maskPixel.A > 0 && maskPixel.R <= 15 && maskPixel.G <= 15 && maskPixel.B <= 15)
                {
                    fixedBlackPixels++;
                }

                if (diffusePixel.A != maskPixel.A)
                {
                    alphaMismatches++;
                }
            }
        }

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
            Assert.That(visibleDiffusePixels, Is.GreaterThan(100));
            Assert.That(redStuffPixels, Is.GreaterThan(100), "The mask needs a visible red Stuff region.");
            Assert.That(alphaMismatches, Is.Zero, "Diffuse and mask silhouettes must match exactly.");
            if (requireFixedBlackRegion)
            {
                Assert.That(
                    fixedBlackPixels,
                    Is.GreaterThan(100),
                    "Fixed handles need a visible black mask region.");
            }
        });
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
