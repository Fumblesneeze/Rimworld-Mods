using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class CeramicsContinuedContractTests
{
    [Test]
    public void Compatibility_patch_adds_one_exact_porcelain_plate_recipe()
    {
        var patchPath = Path.Combine(
            FindRepositoryRoot(),
            "mods",
            "ImmersiveChefs",
            "Patches",
            "Compatibility",
            "zal.ceramics.xml");

        Assert.That(
            File.Exists(patchPath),
            Is.True,
            "Ceramics (Continued) needs a checked-in, package-owned optional recipe patch.");

        var document = XDocument.Load(patchPath);
        var operation = document.Root?.Element("Operation");
        Assert.Multiple(() =>
        {
            Assert.That(
                operation?.Attribute("Class")?.Value,
                Is.EqualTo("PatchOperationFindMod"));
            Assert.That(
                operation?.Element("mods")?.Elements("li").Select(element => element.Value),
                Is.EqualTo(new[] { "Ceramics (Continued)" }));
        });

        var conditional = operation?.Element("match");
        Assert.That(conditional?.Attribute("Class")?.Value, Is.EqualTo("PatchOperationConditional"));
        Assert.Multiple(() =>
        {
            var conditionalPath = conditional?.Element("xpath")?.Value;
            Assert.That(conditionalPath, Does.Contain("N7_Porcelain"));
            Assert.That(conditionalPath, Does.Contain("CeramicsBench_Basic"));
            Assert.That(conditionalPath, Does.Contain("CeramicsBench_Electric"));
            Assert.That(conditionalPath, Does.Contain("BasicCeramics"));
            Assert.That(
                conditional?.Element("nomatch")?.Attribute("Class")?.Value,
                Is.EqualTo("PatchOperationSequence"));
        });
        var recipe = conditional?
            .Descendants("RecipeDef")
            .SingleOrDefault(element => element.Element("defName")?.Value == "ImmersiveChefs_MakePorcelainPlates");
        Assert.That(recipe, Is.Not.Null, "The optional patch must add exactly one porcelain-plate recipe.");
        Assert.Multiple(() =>
        {
            Assert.That(recipe!.Attribute("ParentName")?.Value, Is.EqualTo("ImmersiveChefs_KitchenwareRecipeBase"));
            Assert.That(recipe.Attribute("MayRequire")?.Value, Is.EqualTo("zal.ceramics"));
            Assert.That(recipe.Element("ingredients")?.Elements("li").Single().Element("count")?.Value,
                Is.EqualTo("4"));
            Assert.That(
                recipe.Element("ingredients")?.Elements("li").Single()
                    .Element("filter")?.Element("thingDefs")?.Elements("li").Select(element => element.Value),
                Is.EqualTo(new[] { "N7_Porcelain" }));
            Assert.That(
                recipe.Element("fixedIngredientFilter")?.Element("thingDefs")?.Elements("li")
                    .Select(element => element.Value),
                Is.EqualTo(new[] { "N7_Porcelain" }));
            Assert.That(
                recipe.Element("products")?.Element("ImmersiveChefs_Plate")?.Value,
                Is.EqualTo("4"));
            Assert.That(
                recipe.Element("recipeUsers")?.Elements("li").Select(element => element.Value),
                Is.EqualTo(new[] { "CeramicsBench_Basic", "CeramicsBench_Electric" }));
            Assert.That(recipe.Element("researchPrerequisite")?.Value, Is.EqualTo("BasicCeramics"));
            Assert.That(recipe.Element("productHasIngredientStuff")?.Value, Is.EqualTo("true"));
            var extension = recipe.Element("modExtensions")?.Elements("li").Single();
            Assert.That(extension?.Attribute("Class")?.Value,
                Is.EqualTo("ImmersiveChefs.KitchenwareRecipeExtension"));
            Assert.That(extension?.Element("product")?.Value, Is.EqualTo("Plate"));
            Assert.That(extension?.Element("fabricationTier")?.Value, Is.EqualTo("Ceramic"));
        });
    }

    [Test]
    public void Explicit_porcelain_registration_is_plate_only_and_overrides_stony_fallback()
    {
        var classifier = KitchenMaterialClassifier.CreateDefault();
        classifier.Register("N7_Porcelain", KitchenMaterialKind.Ceramic);
        var descriptor = new KitchenMaterialDescriptor("N7_Porcelain", isStony: true);

        var plate = classifier.Classify(descriptor, KitchenwareProduct.Plate);
        var cookware = classifier.Classify(descriptor, KitchenwareProduct.Cookware);
        var cutlery = classifier.Classify(descriptor, KitchenwareProduct.Cutlery);
        var knife = classifier.Classify(descriptor, KitchenwareProduct.ChefsKnife);

        Assert.Multiple(() =>
        {
            Assert.That(plate?.Kind, Is.EqualTo(KitchenMaterialKind.Ceramic));
            Assert.That(plate?.FabricationTier, Is.EqualTo(FabricationTier.Ceramic));
            Assert.That(
                KitchenMaterialFabricationPolicy.Allows(
                    KitchenwareProduct.Plate,
                    FabricationTier.Ceramic,
                    plate!),
                Is.True);
            Assert.That(cookware, Is.Null);
            Assert.That(cutlery, Is.Null);
            Assert.That(knife, Is.Null);
        });
    }

    [Test]
    public void Catalog_detects_only_the_exact_ceramics_package_and_honors_its_setting()
    {
        var exact = IntegrationCatalog.Detect(new[] { "zal.ceramics" });
        var lookalike = IntegrationCatalog.Detect(new[] { "zal.ceramics.compat" });

        Assert.Multiple(() =>
        {
            Assert.That(exact.IsActive(OptionalIntegration.CeramicsContinued), Is.True);
            Assert.That(lookalike.IsActive(OptionalIntegration.CeramicsContinued), Is.False);
            Assert.That(
                OptionalIntegrationPolicy.IsEnabled(
                    OptionalIntegration.CeramicsContinued,
                    exact,
                    new ImmersiveChefsSettings()),
                Is.True);
            Assert.That(
                OptionalIntegrationPolicy.IsEnabled(
                    OptionalIntegration.CeramicsContinued,
                    exact,
                    new ImmersiveChefsSettings
                    {
                        CeramicsContinued = OptionalIntegrationMode.Off
                    }),
                Is.False);
        });
    }

    [Test]
    public void Product_metadata_orders_after_the_provider_without_making_it_required()
    {
        var about = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "mods",
            "ImmersiveChefs",
            "About",
            "About.xml"));
        var metadata = about.Root!;

        Assert.Multiple(() =>
        {
            Assert.That(
                metadata.Element("loadAfter")?.Elements("li").Select(element => element.Value),
                Does.Contain("zal.ceramics"));
            Assert.That(
                metadata.Element("modDependencies")?.Elements("li")
                    .Select(element => element.Element("packageId")?.Value),
                Does.Not.Contain("zal.ceramics"));
        });
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RimWorldMods.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
