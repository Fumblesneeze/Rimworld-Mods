using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayHttpMessagesTests
{
    [Test]
    public void Request_DTO_is_constructible_without_a_transport_parser()
    {
        var request = new GatewayHttpRequest(
            "POST",
            "/api/v1/example?raw=a%2Fb",
            "/api/v1/example",
            "raw=a%2Fb",
            new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["X-Test"] = "value"
            },
            Encoding.UTF8.GetBytes("payload"));

        Assert.Multiple(() =>
        {
            Assert.That(request.HttpVersion, Is.EqualTo("HTTP/1.1"));
            Assert.That(request.Target, Is.EqualTo("/api/v1/example?raw=a%2Fb"));
            Assert.That(request.Headers["x-test"], Is.EqualTo("value"));
            Assert.That(Encoding.UTF8.GetString(request.Body), Is.EqualTo("payload"));
        });
    }
}
