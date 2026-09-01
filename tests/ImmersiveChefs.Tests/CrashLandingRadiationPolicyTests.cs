using NUnit.Framework;
using System.IO;
using System.Xml.Linq;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class CrashLandingRadiationPolicyTests
{
    private static readonly CrashLandingRadiationShape SupportedShape = new(
        owningPackageId: "Katavrik.CrashLanding",
        defName: "Rad",
        hediffClassName: "Verse.HediffWithComps",
        lethalSeverity: 1f,
        firstVisibleSeverity: 0.1f,
        initialStageHidden: true);

    [Test]
    public void Exact_owned_radiation_def_is_admitted_while_absence_or_disable_uses_Core()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                CrashLandingRadiationPolicy.Decide(
                    packageActive: true,
                    integrationEnabled: true,
                    SupportedShape),
                Is.EqualTo(CrashLandingRadiationOwner.CrashLanding));
            Assert.That(
                CrashLandingRadiationPolicy.Decide(
                    packageActive: false,
                    integrationEnabled: true,
                    SupportedShape),
                Is.EqualTo(CrashLandingRadiationOwner.CoreToxicBuildup));
            Assert.That(
                CrashLandingRadiationPolicy.Decide(
                    packageActive: true,
                    integrationEnabled: false,
                    SupportedShape),
                Is.EqualTo(CrashLandingRadiationOwner.CoreToxicBuildup));
        });
    }

    [TestCase("Changed.Package", "Rad", "Verse.HediffWithComps", 1f, 0.1f, true)]
    [TestCase("Katavrik.CrashLanding", "Changed", "Verse.HediffWithComps", 1f, 0.1f, true)]
    [TestCase("Katavrik.CrashLanding", "Rad", "Verse.Hediff", 1f, 0.1f, true)]
    [TestCase("Katavrik.CrashLanding", "Rad", "Verse.HediffWithComps", 2f, 0.1f, true)]
    [TestCase("Katavrik.CrashLanding", "Rad", "Verse.HediffWithComps", 1f, 0.2f, true)]
    [TestCase("Katavrik.CrashLanding", "Rad", "Verse.HediffWithComps", 1f, 0.1f, false)]
    public void Changed_or_foreign_def_shape_fails_closed_to_Core(
        string packageId,
        string defName,
        string hediffClassName,
        float lethalSeverity,
        float firstVisibleSeverity,
        bool initialStageHidden)
    {
        var changed = new CrashLandingRadiationShape(
            packageId,
            defName,
            hediffClassName,
            lethalSeverity,
            firstVisibleSeverity,
            initialStageHidden);

        Assert.That(
            CrashLandingRadiationPolicy.Decide(
                packageActive: true,
                integrationEnabled: true,
                changed),
            Is.EqualTo(CrashLandingRadiationOwner.CoreToxicBuildup));
    }

    [Test]
    public void Provider_priority_is_Rimatomics_then_CrashLanding_then_Core()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                UraniumRadiationProviderPolicy.Decide(
                    rimatomicsSupported: true,
                    crashLandingSupported: true),
                Is.EqualTo(UraniumRadiationOwner.Rimatomics));
            Assert.That(
                UraniumRadiationProviderPolicy.Decide(
                    rimatomicsSupported: false,
                    crashLandingSupported: true),
                Is.EqualTo(UraniumRadiationOwner.CrashLanding));
            Assert.That(
                UraniumRadiationProviderPolicy.Decide(
                    rimatomicsSupported: false,
                    crashLandingSupported: false),
                Is.EqualTo(UraniumRadiationOwner.CoreToxicBuildup));
        });
    }

    [Test]
    public void Integration_is_optional_setting_controlled_and_loads_after_CrashLanding()
    {
        var snapshot = IntegrationCatalog.Detect(new[] { CrashLandingRadiationPolicy.PackageId });
        var enabled = new ImmersiveChefsSettings();
        var disabled = new ImmersiveChefsSettings { CrashLanding = OptionalIntegrationMode.Off };
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
                OptionalIntegrationPolicy.IsEnabled(OptionalIntegration.CrashLanding, snapshot, enabled),
                Is.True);
            Assert.That(
                OptionalIntegrationPolicy.IsEnabled(OptionalIntegration.CrashLanding, snapshot, disabled),
                Is.False);
            Assert.That(loadAfter, Does.Contain(CrashLandingRadiationPolicy.PackageId));
            Assert.That(required, Does.Not.Contain(CrashLandingRadiationPolicy.PackageId));
        });
    }

    [Test]
    public void Compatibility_warning_gate_can_be_claimed_only_once()
    {
        var state = 0;

        Assert.Multiple(() =>
        {
            Assert.That(CrashLandingRadiationPolicy.TryClaimWarning(ref state), Is.True);
            Assert.That(CrashLandingRadiationPolicy.TryClaimWarning(ref state), Is.False);
            Assert.That(state, Is.EqualTo(1));
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
