using System.IO;
using System.Xml.Linq;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class MealTextureOwnershipTests
{
    private static readonly string[] OptionalGraphicOwners =
    {
        "Thekiborg.DMTR",
        "Goat.Food.Texture.Variety.Core",
        "Goat.Food.Texture.Variety",
        "Goat.Food.Texture.Variety.VECooking",
        "Goat.Food.Texture.Variety.VEStew",
        "Goat.Food.Texture.Variety.VESushi"
    };

    [Test]
    public void Mod_metadata_loads_after_every_supported_meal_graphic_owner_without_requiring_it()
    {
        var root = FindRepositoryRoot();
        var project = XDocument.Load(Path.Combine(root, "mods", "ImmersiveChefs", "ImmersiveChefs.csproj"));
        XNamespace ns = project.Root?.Name.Namespace ?? XNamespace.None;
        var loadAfter = project.Descendants(ns + "RimWorldLoadAfter")
            .Select(element => (string?)element.Attribute("Include"))
            .ToArray();
        var required = project.Descendants(ns + "RimWorldModDependency")
            .Select(element => (string?)element.Attribute("Include"))
            .ToArray();

        Assert.Multiple(() =>
        {
            foreach (var packageId in OptionalGraphicOwners)
            {
                Assert.That(loadAfter, Does.Contain(packageId), packageId);
                Assert.That(required, Does.Not.Contain(packageId), packageId);
            }
        });
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "RimWorldMods.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Immersive Chefs repository root.");
    }
}
