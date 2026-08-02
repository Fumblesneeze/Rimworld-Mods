using System;
using System.Reflection;
using RimWorldDevGateway.IntegrationTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayIntegrationTestingContractTests
{
    [Test]
    public void Attribute_declares_the_requested_in_game_lifecycle_point()
    {
        var method = typeof(ContractFixture).GetMethod(
            nameof(ContractFixture.AfterMainMenuLoad),
            BindingFlags.Public | BindingFlags.Static);

        var attribute = method!.GetCustomAttribute<IntegrationTestAttribute>();

        Assert.That(attribute, Is.Not.Null);
        Assert.That(attribute!.RunAt, Is.EqualTo(RunAt.MainMenuLoaded));
    }

    [Test]
    public void Assertion_helpers_fail_with_a_contract_specific_exception()
    {
        var error = Assert.Throws<IntegrationTestAssertionException>(() =>
            IntegrationAssert.Equal("expected", "actual", "patched field"));

        Assert.That(error!.Message, Does.Contain("patched field"));
        Assert.That(error.Message, Does.Contain("expected"));
        Assert.That(error.Message, Does.Contain("actual"));
    }

    private static class ContractFixture
    {
        [IntegrationTest(RunAt.MainMenuLoaded)]
        public static void AfterMainMenuLoad()
        {
        }
    }
}
