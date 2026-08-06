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

    [TestCase("rabiosus.AdaptiveMealBill", OptionalIntegration.AdaptiveMealBill)]
    [TestCase("binchcannon.overcookedmeals", OptionalIntegration.OvercookedMeals)]
    [TestCase("Memegoddess.MealsOnWheels", OptionalIntegration.MealsOnWheels)]
    [TestCase("seekiworksmod.no10", OptionalIntegration.PrioritizeMeals)]
    [TestCase("sumghai.Replimat", OptionalIntegration.Replimat)]
    [TestCase("Mlie.MealPrinter", OptionalIntegration.MealPrinter)]
    public void Detect_marks_exact_optional_compatibility_integrations_active(
        string packageId,
        OptionalIntegration integration)
    {
        var snapshot = IntegrationCatalog.Detect(new[] { packageId });

        Assert.That(snapshot.IsActive(integration), Is.True);
    }

    [TestCase("rabiosus.AdaptiveMealBill.lookalike")]
    [TestCase("binchcannon.overcookedmeals.compat")]
    [TestCase("Memegoddess.MealsOnWheels.compat")]
    [TestCase("seekiworksmod.no100")]
    [TestCase("sumghai.ReplimatMeals")]
    [TestCase("Mlie.MealPrinterPlus")]
    public void Detect_ignores_lookalike_optional_compatibility_packages(string packageId)
    {
        var snapshot = IntegrationCatalog.Detect(new[] { packageId });

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.IsActive(OptionalIntegration.AdaptiveMealBill), Is.False);
            Assert.That(snapshot.IsActive(OptionalIntegration.OvercookedMeals), Is.False);
            Assert.That(snapshot.IsActive(OptionalIntegration.MealsOnWheels), Is.False);
            Assert.That(snapshot.IsActive(OptionalIntegration.PrioritizeMeals), Is.False);
            Assert.That(snapshot.IsActive(OptionalIntegration.Replimat), Is.False);
            Assert.That(snapshot.IsActive(OptionalIntegration.MealPrinter), Is.False);
        });
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

    [TestCase(OptionalIntegration.AdaptiveMealBill)]
    [TestCase(OptionalIntegration.OvercookedMeals)]
    [TestCase(OptionalIntegration.MealsOnWheels)]
    [TestCase(OptionalIntegration.PrioritizeMeals)]
    [TestCase(OptionalIntegration.Replimat)]
    [TestCase(OptionalIntegration.MealPrinter)]
    public void Disabled_optional_compatibility_setting_prevents_activation_when_loaded(
        OptionalIntegration integration)
    {
        var snapshot = IntegrationCatalog.Detect(new[]
        {
            "rabiosus.AdaptiveMealBill",
            "binchcannon.overcookedmeals",
            "Memegoddess.MealsOnWheels",
            "seekiworksmod.no10",
            "sumghai.Replimat",
            "sumghai.ReplimatMeals",
            "Mlie.MealPrinter"
        });
        var settings = new ImmersiveChefsSettings
        {
            AdaptiveMealBill = integration == OptionalIntegration.AdaptiveMealBill
                ? OptionalIntegrationMode.Off
                : OptionalIntegrationMode.Auto,
            OvercookedMeals = integration == OptionalIntegration.OvercookedMeals
                ? OptionalIntegrationMode.Off
                : OptionalIntegrationMode.Auto,
            MealsOnWheels = integration == OptionalIntegration.MealsOnWheels
                ? OptionalIntegrationMode.Off
                : OptionalIntegrationMode.Auto,
            PrioritizeMeals = integration == OptionalIntegration.PrioritizeMeals
                ? OptionalIntegrationMode.Off
                : OptionalIntegrationMode.Auto,
            Replimat = integration == OptionalIntegration.Replimat
                ? OptionalIntegrationMode.Off
                : OptionalIntegrationMode.Auto,
            MealPrinter = integration == OptionalIntegration.MealPrinter
                ? OptionalIntegrationMode.Off
                : OptionalIntegrationMode.Auto
        };

        Assert.That(OptionalIntegrationPolicy.IsEnabled(integration, snapshot, settings), Is.False);
    }

    [TestCaseSource(nameof(UnknownOrEmptyPackageSets))]
    public void Detect_returns_a_complete_inactive_snapshot_for_unknown_or_empty_packages(string[] packageIds)
    {
        var snapshot = IntegrationCatalog.Detect(packageIds);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.States, Has.Count.EqualTo(19));
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

    [Test]
    public void Replimat_adapter_requires_the_exact_meal_addon()
    {
        var baseOnly = IntegrationCatalog.Detect(new[] { "sumghai.Replimat" });
        var complete = IntegrationCatalog.Detect(new[]
        {
            "sumghai.Replimat",
            "sumghai.ReplimatMeals"
        });

        Assert.Multiple(() =>
        {
            Assert.That(
                OptionalIntegrationPolicy.IsEnabled(
                    OptionalIntegration.Replimat,
                    baseOnly,
                    new ImmersiveChefsSettings()),
                Is.False);
            Assert.That(
                OptionalIntegrationPolicy.IsEnabled(
                    OptionalIntegration.Replimat,
                    complete,
                    new ImmersiveChefsSettings()),
                Is.True);
        });
    }
}
