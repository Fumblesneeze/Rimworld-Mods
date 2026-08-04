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
}
