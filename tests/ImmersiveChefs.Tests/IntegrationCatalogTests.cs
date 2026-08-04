using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class IntegrationCatalogTests
{
    private static readonly string[][] UnknownOrEmptyPackageSets =
    {
        Array.Empty<string>(),
        new[] { "someone.elses.mod" }
    };

    [Test]
    public void Detect_marks_a_recognized_loaded_integration_active()
    {
        var snapshot = IntegrationCatalog.Detect(new[] { "dubwise.dubsbadhygiene" });

        Assert.That(snapshot.IsActive(OptionalIntegration.DubsBadHygiene), Is.True);
    }

    [Test]
    public void Detect_marks_the_loaded_hospitality_package_active()
    {
        var snapshot = IntegrationCatalog.Detect(new[] { "orion.hospitality" });

        Assert.That(snapshot.IsActive(OptionalIntegration.Hospitality), Is.True);
    }

    [Test]
    public void Detect_marks_the_loaded_common_sense_package_active()
    {
        var snapshot = IntegrationCatalog.Detect(new[] { "avilmask.commonsense" });

        Assert.That(snapshot.IsActive(OptionalIntegration.CommonSense), Is.True);
    }

    [Test]
    public void Disabled_common_sense_setting_prevents_activation_when_loaded()
    {
        var snapshot = IntegrationCatalog.Detect(new[] { "avilmask.commonsense" });
        var settings = new ImmersiveChefsSettings
        {
            CommonSense = OptionalIntegrationMode.Off
        };

        Assert.That(
            OptionalIntegrationPolicy.IsEnabled(OptionalIntegration.CommonSense, snapshot, settings),
            Is.False);
    }

    [Test]
    public void Disabled_hospitality_setting_prevents_activation_when_loaded()
    {
        var snapshot = IntegrationCatalog.Detect(new[] { "orion.hospitality" });
        var settings = new ImmersiveChefsSettings
        {
            Hospitality = OptionalIntegrationMode.Off
        };

        Assert.That(
            OptionalIntegrationPolicy.IsEnabled(OptionalIntegration.Hospitality, snapshot, settings),
            Is.False);
    }

    [TestCaseSource(nameof(UnknownOrEmptyPackageSets))]
    public void Detect_returns_a_complete_inactive_snapshot_for_unknown_or_empty_packages(string[] packageIds)
    {
        var snapshot = IntegrationCatalog.Detect(packageIds);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.States, Has.Count.EqualTo(13));
            Assert.That(snapshot.States.Values, Has.All.False);
        });
    }

    [Test]
    public void Detect_normalizes_duplicates_and_returns_immutable_results()
    {
        var snapshot = IntegrationCatalog.Detect(new[]
        {
            "DUBWISE.DUBSBADHYGIENE",
            "dubwise.dubsbadhygiene",
            "SYRCHALIS.PROCESSOR.FRAMEWORK"
        });

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Active, Is.EquivalentTo(new[]
            {
                OptionalIntegration.DubsBadHygiene,
                OptionalIntegration.ProcessorFramework
            }));
            Assert.That(snapshot.Active, Has.Count.EqualTo(2));
            Assert.That(
                () => ((IDictionary<OptionalIntegration, bool>)snapshot.States)[OptionalIntegration.Royalty] = true,
                Throws.TypeOf<NotSupportedException>());
        });
    }

    [Test]
    public void DisabledProcessorFrameworkDoesNotActivateEvenWhenLoaded()
    {
        var snapshot = IntegrationCatalog.Detect(new[] { "syrchalis.processor.framework" });
        var settings = new ImmersiveChefsSettings
        {
            ProcessorFramework = OptionalIntegrationMode.Off
        };

        Assert.That(
            OptionalIntegrationPolicy.IsEnabled(
                OptionalIntegration.ProcessorFramework,
                snapshot,
                settings),
            Is.False);
    }

    [Test]
    public void GastronomyRequiresItsActiveCashRegisterDependency()
    {
        var gastronomyOnly = IntegrationCatalog.Detect(new[] { "orion.gastronomy" });
        var complete = IntegrationCatalog.Detect(new[]
        {
            "orion.gastronomy",
            "orion.cashregister"
        });

        Assert.Multiple(() =>
        {
            Assert.That(
                OptionalIntegrationPolicy.IsEnabled(
                    OptionalIntegration.Gastronomy,
                    gastronomyOnly,
                    new ImmersiveChefsSettings()),
                Is.False);
            Assert.That(
                OptionalIntegrationPolicy.IsEnabled(
                    OptionalIntegration.Gastronomy,
                    complete,
                    new ImmersiveChefsSettings()),
                Is.True);
        });
    }

    [Test]
    public void NutrientPasteExpandedRequiresVanillaExpandedFramework()
    {
        var pasteOnly = IntegrationCatalog.Detect(new[] { "vanillaexpanded.vnutriente" });
        var complete = IntegrationCatalog.Detect(new[]
        {
            "vanillaexpanded.vnutriente",
            "oskarpotocki.vanillafactionsexpanded.core"
        });

        Assert.That(
            OptionalIntegrationPolicy.IsEnabled(
                OptionalIntegration.VanillaNutrientPasteExpanded,
                pasteOnly,
                new ImmersiveChefsSettings()),
            Is.False);
        Assert.That(
            OptionalIntegrationPolicy.IsEnabled(
                OptionalIntegration.VanillaNutrientPasteExpanded,
                complete,
                new ImmersiveChefsSettings()),
            Is.True);
    }
}
