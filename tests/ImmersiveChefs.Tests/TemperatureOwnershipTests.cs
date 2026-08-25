using System.IO;
using System.Xml.Linq;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class TemperatureOwnershipTests
{
    [Test]
    public void Thermodynamics_package_owns_temperature_even_when_shape_validation_fails()
    {
        var validShape = TemperatureOwnership.Decide(
            new[] { "Mlie.DThermodynamicsHotMeals" },
            thermodynamicsShapeValid: true);
        var changedShape = TemperatureOwnership.Decide(
            new[] { "mlie.dthermodynamicshotmeals" },
            thermodynamicsShapeValid: false);

        Assert.Multiple(() =>
        {
            Assert.That(validShape.Owner, Is.EqualTo(MealTemperatureOwner.Thermodynamics));
            Assert.That(validShape.ShapeWarningRequired, Is.False);
            Assert.That(changedShape.Owner, Is.EqualTo(MealTemperatureOwner.Thermodynamics));
            Assert.That(changedShape.ShapeWarningRequired, Is.True);
            Assert.That(changedShape.ImmersiveChefsFeaturesActive, Is.False);
        });
    }

    [Test]
    public void Immersive_chefs_owns_temperature_only_when_thermodynamics_is_absent()
    {
        var decision = TemperatureOwnership.Decide(
            new[] { "brrainz.harmony", "fumblesneeze.immersivechefs" },
            thermodynamicsShapeValid: false);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Owner, Is.EqualTo(MealTemperatureOwner.ImmersiveChefs));
            Assert.That(decision.ShapeWarningRequired, Is.False);
            Assert.That(decision.ImmersiveChefsFeaturesActive, Is.True);
        });
    }

    [Test]
    public void Thermodynamics_patch_adds_then_removes_the_fallback_by_exact_package_before_def_deserialization()
    {
        var root = FindRepositoryRoot();
        var oldUnconditionalPath = Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Defs",
            "ThingDefs",
            "Microwave.xml");
        var document = XDocument.Load(Path.Combine(
            root,
            "mods",
            "ImmersiveChefs",
            "Patches",
            "Compatibility",
            "ThermodynamicsHotMeals.xml"));
        var operations = document.Root?
            .Element("Operation")?
            .Element("operations")?
            .Elements("li")
            .ToList() ?? new List<XElement>();
        var add = operations.Single(operation =>
            (string?)operation.Attribute("Class") == "PatchOperationAdd");
        var guardedRemove = operations.Single(operation =>
            (string?)operation.Attribute("Class") == "ImmersiveChefs.PatchOperationRemoveIfModActive");
        var addIndex = operations.IndexOf(add);
        var guardedRemoveIndex = operations.IndexOf(guardedRemove);

        Assert.Multiple(() =>
        {
            Assert.That(
                (string?)add.Attribute("Class"),
                Is.EqualTo("PatchOperationAdd"));
            Assert.That(
                (string?)add.Element("xpath"),
                Is.EqualTo("/Defs"));
            Assert.That(
                (string?)add.Element("value")?.Element("ThingDef")?.Element("defName"),
                Is.EqualTo("ImmersiveChefs_Microwave"));
            Assert.That(
                (string?)guardedRemove.Attribute("Class"),
                Is.EqualTo("ImmersiveChefs.PatchOperationRemoveIfModActive"));
            Assert.That(
                (string?)guardedRemove.Element("packageId"),
                Is.EqualTo(TemperatureOwnership.ThermodynamicsPackageId));
            Assert.That(
                (string?)guardedRemove.Element("xpath"),
                Is.EqualTo("/Defs/ThingDef[defName=\"ImmersiveChefs_Microwave\"]"));
            Assert.That(addIndex, Is.LessThan(guardedRemoveIndex));
            Assert.That(File.Exists(oldUnconditionalPath), Is.False);
        });
    }

    [Test]
    public void Mod_metadata_loads_after_thermodynamics_without_declaring_it_required()
    {
        var root = FindRepositoryRoot();
        var project = XDocument.Load(Path.Combine(root, "mods", "ImmersiveChefs", "ImmersiveChefs.csproj"));
        XNamespace ns = project.Root?.Name.Namespace ?? XNamespace.None;

        Assert.Multiple(() =>
        {
            Assert.That(
                project.Descendants(ns + "RimWorldLoadAfter")
                    .Select(element => (string?)element.Attribute("Include")),
                Does.Contain(TemperatureOwnership.ThermodynamicsPackageId));
            Assert.That(
                project.Descendants(ns + "RimWorldModDependency")
                    .Select(element => (string?)element.Attribute("Include")),
                Does.Not.Contain(TemperatureOwnership.ThermodynamicsPackageId));
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
