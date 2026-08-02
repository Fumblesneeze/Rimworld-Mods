using System.Net;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayAuthenticatorTests
{
    [Test]
    public void Authorize_accepts_only_the_exact_bearer_token_from_loopback()
    {
        const string token = "development-session-token";

        Assert.Multiple(() =>
        {
            Assert.That(
                GatewayAuthenticator.Authorize(
                    new IPEndPoint(IPAddress.Loopback, 31000),
                    "Bearer " + token,
                    token),
                Is.True);
            Assert.That(
                GatewayAuthenticator.Authorize(
                    new IPEndPoint(IPAddress.Parse("192.0.2.12"), 31000),
                    "Bearer " + token,
                    token),
                Is.False);
            Assert.That(
                GatewayAuthenticator.Authorize(
                    new IPEndPoint(IPAddress.Loopback, 31000),
                    "Bearer wrong-token",
                    token),
                Is.False);
        });
    }
}
