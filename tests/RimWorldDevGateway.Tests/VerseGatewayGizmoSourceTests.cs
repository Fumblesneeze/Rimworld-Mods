using System;
using System.Collections.Generic;
using System.Linq;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class VerseGatewayGizmoSourceTests
{
    [Test]
    public void Broken_owner_is_logged_with_request_correlation_without_hiding_healthy_owners()
    {
        var logs = new GatewayLogBuffer();
        var source = new VerseGatewayGizmoSource(logs);
        var visited = new List<string>();
        GatewayRequestScope.CurrentRequestId = "gizmo-owner-request";

        try
        {
            source.DiscoverOwnersIndependently(
                new[] { "broken-owner", "healthy-owner" },
                owner =>
                {
                    if (owner == "broken-owner")
                    {
                        throw new InvalidOperationException("modded GetGizmos failed");
                    }

                    visited.Add(owner);
                    return true;
                },
                owner => owner);
        }
        finally
        {
            GatewayRequestScope.CurrentRequestId = null;
        }

        var diagnostic = logs.Read(0, 10).Entries.Single();
        Assert.Multiple(() =>
        {
            Assert.That(visited, Is.EqualTo(new[] { "healthy-owner" }));
            Assert.That(diagnostic.Severity, Is.EqualTo("Warning"));
            Assert.That(diagnostic.RequestId, Is.EqualTo("gizmo-owner-request"));
            Assert.That(diagnostic.Message, Does.Contain("broken-owner"));
            Assert.That(diagnostic.Message, Does.Contain("GetGizmos"));
            Assert.That(diagnostic.Stack, Does.Contain(nameof(InvalidOperationException)));
            Assert.That(diagnostic.Stack, Does.Contain("modded GetGizmos failed"));
        });
    }
}
