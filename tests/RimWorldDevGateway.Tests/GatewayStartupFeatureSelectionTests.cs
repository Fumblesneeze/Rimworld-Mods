using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayStartupFeatureSelectionTests
{
    [Test]
    public void End_to_end_loading_is_disabled_without_its_exact_startup_flag()
    {
        var queried = new List<string>();
        var selection = GatewayStartupFeatureSelection.Capture(argument =>
        {
            queried.Add(argument);
            return false;
        });

        Assert.Multiple(() =>
        {
            Assert.That(selection.RunEndToEndTests, Is.False);
            Assert.That(queried, Does.Contain(GatewayStartupFeatureSelection.EndToEndFlag));
        });
    }

    [Test]
    public void Integration_and_e2e_startup_flags_are_independent()
    {
        var onlyIntegration = GatewayStartupFeatureSelection.Capture(
            argument => argument == GatewayStartupFeatureSelection.IntegrationFlag);
        var onlyEndToEnd = GatewayStartupFeatureSelection.Capture(
            argument => argument == GatewayStartupFeatureSelection.EndToEndFlag);

        Assert.Multiple(() =>
        {
            Assert.That(onlyIntegration.RunIntegrationTests, Is.True);
            Assert.That(onlyIntegration.RunEndToEndTests, Is.False);
            Assert.That(onlyEndToEnd.RunIntegrationTests, Is.False);
            Assert.That(onlyEndToEnd.RunEndToEndTests, Is.True);
        });
    }

    [Test]
    public void End_to_end_test_selection_is_exact_sorted_and_restart_bound()
    {
        var selection = GatewayStartupFeatureSelection.Capture(
            argument => argument == GatewayStartupFeatureSelection.EndToEndFlag,
            argument => argument == GatewayStartupFeatureSelection.EndToEndTestIdsArgument
                ? "beta.test,alpha.test"
                : null);

        Assert.That(selection.SelectedEndToEndTestIds,
            Is.EqualTo(new[] { "alpha.test", "beta.test" }));
    }

    [Test]
    public void End_to_end_test_selection_rejects_duplicate_ids()
    {
        Assert.Throws<ArgumentException>(() => GatewayStartupFeatureSelection.Capture(
            argument => argument == GatewayStartupFeatureSelection.EndToEndFlag,
            argument => argument == GatewayStartupFeatureSelection.EndToEndTestIdsArgument
                ? "alpha.test,alpha.test"
                : null));
    }
}
