using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class PortableWareMaterialArtTests
{
    [Test]
    public void Base_stuffable_ware_consumes_its_material_mask()
    {
        var root = FindRepositoryRoot();
        var defs = XDocument.Load(Path.Combine(root, "mods", "ImmersiveChefs", "Defs", "ThingDefs", "Kitchenware.xml"));
        foreach (var defName in new[]
                 {
                     "ImmersiveChefs_Cookware", "ImmersiveChefs_Plate", "ImmersiveChefs_Cutlery",
                     "ImmersiveChefs_ChefsKnife"
                 })
        {
            var def = defs.Root!.Elements("ThingDef")
                .Single(element => (string?)element.Element("defName") == defName);
            Assert.That((string?)def.Element("graphicData")?.Element("shaderType"), Is.EqualTo("CutoutComplex"),
                defName + " must actually consume its packaged Stuff mask.");
        }
    }

    [TestCase("Plate/Plate")]
    [TestCase("Cutlery/Cutlery")]
    [TestCase("Cookware/Cookware")]
    public void Material_bearing_pixels_are_light_enough_for_core_steel_tint(string relativeAsset)
    {
        var root = FindRepositoryRoot();
        var basePath = Path.Combine(root, "mods", "ImmersiveChefs", "Textures", "ImmersiveChefs", "Things", "Item", "Kitchenware");
        using var diffuse = new Bitmap(Path.Combine(basePath, relativeAsset.Replace('/', Path.DirectorySeparatorChar) + ".png"));
        using var mask = new Bitmap(Path.Combine(basePath, relativeAsset.Replace('/', Path.DirectorySeparatorChar) + "_m.png"));
        var values = new List<double>();
        for (var y = 0; y < diffuse.Height; y++)
        for (var x = 0; x < diffuse.Width; x++)
        {
            var material = mask.GetPixel(x, y);
            var color = diffuse.GetPixel(x, y);
            if (color.A >= 96 && material.R >= 200 && material.G <= 55 && material.B <= 55)
                values.Add(0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B);
        }

        Assert.Multiple(() =>
        {
            Assert.That(values.Count, Is.GreaterThan(128), relativeAsset + " needs a meaningful Stuff-colored region.");
            Assert.That(values.Average(), Is.GreaterThanOrEqualTo(210d),
                relativeAsset + " double-darkens Core steel because its already-dark diffuse is multiplied by Steel's 105/255 Stuff color.");
        });
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "RimWorldMods.sln"))) return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the Immersive Chefs repository root.");
    }
}
