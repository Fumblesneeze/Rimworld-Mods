using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Verse;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class VerseGatewayGizmoSourceTests
{
    [Test]
    public void Newly_opened_float_menu_is_held_and_leased_for_minimized_semantic_automation()
    {
        var existing = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));
        var opened = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));
        existing.vanishIfMouseDistant = true;
        opened.vanishIfMouseDistant = true;

        FloatMenuAutomationLease.CaptureNewlyOpened(
            new[] { existing },
            new[] { existing, opened });

        try
        {
            Assert.Multiple(() =>
            {
                Assert.That(existing.vanishIfMouseDistant, Is.True,
                    "Pre-existing player windows must not be changed.");
                Assert.That(opened.vanishIfMouseDistant, Is.False,
                    "A native menu opened by the semantic gizmo action must remain visible while minimized.");
                Assert.That(FloatMenuAutomationLease.CapturedForAutomation, Is.SameAs(opened),
                    "The following current-menu step must be bound to the exact newly opened window.");
            });
        }
        finally
        {
            FloatMenuAutomationLease.Clear();
        }
    }

    [Test]
    public void Ambiguous_new_float_menus_do_not_publish_an_automation_lease()
    {
        var first = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));
        var second = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));

        FloatMenuAutomationLease.CaptureNewlyOpened(
            Array.Empty<FloatMenu>(),
            new[] { first, second });

        try
        {
            Assert.That(FloatMenuAutomationLease.CapturedForAutomation, Is.Null);
        }
        finally
        {
            FloatMenuAutomationLease.Clear();
        }
    }

    [Test]
    public void Clearing_the_float_menu_lease_restores_its_original_mouse_distance_behavior()
    {
        var opened = (FloatMenu)FormatterServices.GetUninitializedObject(typeof(FloatMenu));
        opened.vanishIfMouseDistant = true;

        FloatMenuAutomationLease.CaptureNewlyOpened(
            Array.Empty<FloatMenu>(),
            new[] { opened });
        Assert.That(opened.vanishIfMouseDistant, Is.False,
            "The active automation lease must hold the menu open.");

        FloatMenuAutomationLease.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(opened.vanishIfMouseDistant, Is.True,
                "Releasing a failed or abandoned lease must restore the native behavior.");
            Assert.That(FloatMenuAutomationLease.CapturedForAutomation, Is.Null);
        });
    }

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
