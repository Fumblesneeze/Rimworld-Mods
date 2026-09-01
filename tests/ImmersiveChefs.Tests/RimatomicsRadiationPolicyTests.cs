using NUnit.Framework;
using System.IO;
using System.Xml.Linq;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class RimatomicsRadiationPolicyTests
{
    private static readonly string[][] ChangedParameterShapes =
    {
        new[] { "System.Object", "System.Single" },
        new[] { "Verse.Pawn" },
        new[] { "Verse.Pawn", "System.Double" }
    };

    private static readonly RimatomicsRadiationShape SupportedShape = new(
        assemblyName: "Rimatomics",
        typeName: "Rimatomics.DubUtils",
        methodName: "applyRads",
        isPublic: true,
        isStatic: true,
        returnTypeName: "System.Void",
        parameterTypeNames: new[] { "Verse.Pawn", "System.Single" });

    [Test]
    public void Supported_exact_shape_selects_Rimatomics_while_absence_or_disable_uses_Core()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                RimatomicsRadiationPolicy.Decide(packageActive: true, integrationEnabled: true, SupportedShape),
                Is.EqualTo(RimatomicsRadiationOwner.Rimatomics));
            Assert.That(
                RimatomicsRadiationPolicy.Decide(packageActive: false, integrationEnabled: true, SupportedShape),
                Is.EqualTo(RimatomicsRadiationOwner.CoreToxicBuildup));
            Assert.That(
                RimatomicsRadiationPolicy.Decide(packageActive: true, integrationEnabled: false, SupportedShape),
                Is.EqualTo(RimatomicsRadiationOwner.CoreToxicBuildup));
        });
    }

    [TestCase("Changed", "Rimatomics.DubUtils", "applyRads", true, true, "System.Void")]
    [TestCase("Rimatomics", "Changed.DubUtils", "applyRads", true, true, "System.Void")]
    [TestCase("Rimatomics", "Rimatomics.DubUtils", "ApplyRads", true, true, "System.Void")]
    [TestCase("Rimatomics", "Rimatomics.DubUtils", "applyRads", false, true, "System.Void")]
    [TestCase("Rimatomics", "Rimatomics.DubUtils", "applyRads", true, false, "System.Void")]
    [TestCase("Rimatomics", "Rimatomics.DubUtils", "applyRads", true, true, "System.Boolean")]
    public void Changed_shape_fails_closed_to_Core_fallback(
        string assemblyName,
        string typeName,
        string methodName,
        bool isPublic,
        bool isStatic,
        string returnTypeName)
    {
        var changed = new RimatomicsRadiationShape(
            assemblyName,
            typeName,
            methodName,
            isPublic,
            isStatic,
            returnTypeName,
            new[] { "Verse.Pawn", "System.Single" });

        Assert.That(
            RimatomicsRadiationPolicy.Decide(packageActive: true, integrationEnabled: true, changed),
            Is.EqualTo(RimatomicsRadiationOwner.CoreToxicBuildup));
    }

    [TestCaseSource(nameof(ChangedParameterShapes))]
    public void Changed_parameter_shape_fails_closed_to_Core_fallback(string[] parameters)
    {
        var changed = new RimatomicsRadiationShape(
            "Rimatomics",
            "Rimatomics.DubUtils",
            "applyRads",
            true,
            true,
            "System.Void",
            parameters);

        Assert.That(
            RimatomicsRadiationPolicy.Decide(packageActive: true, integrationEnabled: true, changed),
            Is.EqualTo(RimatomicsRadiationOwner.CoreToxicBuildup));
    }

    [TestCase(0.020f, 6.954506f)]
    [TestCase(0.015f, 5.215880f)]
    [TestCase(0.010f, 3.477253f)]
    [TestCase(0f, 0f)]
    public void Severity_equivalent_dose_is_calibrated_to_upstream_strength(float dose, float expected)
    {
        Assert.That(
            RimatomicsRadiationPolicy.StrengthForSeverity(dose),
            Is.EqualTo(expected).Within(0.00001f));
    }

    [Test]
    public void Compatibility_warning_gate_can_be_claimed_only_once()
    {
        var state = 0;

        Assert.Multiple(() =>
        {
            Assert.That(RimatomicsRadiationPolicy.TryClaimWarning(ref state), Is.True);
            Assert.That(RimatomicsRadiationPolicy.TryClaimWarning(ref state), Is.False);
            Assert.That(state, Is.EqualTo(1));
        });
    }

    [Test]
    public void Integration_is_optional_setting_controlled_and_loads_after_Rimatomics()
    {
        var snapshot = IntegrationCatalog.Detect(new[] { RimatomicsRadiationPolicy.PackageId });
        var enabled = new ImmersiveChefsSettings();
        var disabled = new ImmersiveChefsSettings { Rimatomics = OptionalIntegrationMode.Off };
        var project = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "mods",
            "ImmersiveChefs",
            "ImmersiveChefs.csproj"));
        var loadAfter = project.Descendants("RimWorldLoadAfter")
            .Select(element => (string?)element.Attribute("Include"));
        var required = project.Descendants("RimWorldSteamModDependency")
            .Select(element => (string?)element.Attribute("Include"));

        Assert.Multiple(() =>
        {
            Assert.That(
                OptionalIntegrationPolicy.IsEnabled(OptionalIntegration.Rimatomics, snapshot, enabled),
                Is.True);
            Assert.That(
                OptionalIntegrationPolicy.IsEnabled(OptionalIntegration.Rimatomics, snapshot, disabled),
                Is.False);
            Assert.That(loadAfter, Does.Contain(RimatomicsRadiationPolicy.PackageId));
            Assert.That(required, Does.Not.Contain(RimatomicsRadiationPolicy.PackageId));
        });
    }

    private static string FindRepositoryRoot()
    {
        var cursor = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (cursor is not null && !File.Exists(Path.Combine(cursor.FullName, "RimWorldMods.sln")))
        {
            cursor = cursor.Parent;
        }

        return cursor?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
