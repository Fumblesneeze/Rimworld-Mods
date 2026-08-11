using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayAssemblyRuntimeExtensionsTests
{
    [Test]
    public void Registered_automation_is_session_scoped_and_runs_with_normalized_arguments()
    {
        var registry = new GatewayAutomationRegistry(runIdFactory: () => "assembly-run");
        var extensions = new GatewayAssemblyRuntimeExtensions(registry);
        var received = string.Empty;
        extensions.RegisterSessionAutomation(
            new GatewayAssemblyAutomationDescriptor(
                "uploaded.fixture",
                "2",
                "Runs an uploaded fixture.",
                mutating: true,
                argumentSchema: new Dictionary<string, string>
                {
                    ["count"] = "integer"
                },
                prerequisites: new[] { "A playable map" }),
            (argumentsJson, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                received = argumentsJson;
                return "registered-result";
            });

        var descriptor = registry.Describe().Single();
        var run = registry.StartRun(
            "uploaded.fixture",
            "registered-automation-test",
            new Dictionary<string, object?> { ["count"] = 3 });

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Name, Is.EqualTo("uploaded.fixture"));
            Assert.That(descriptor.Version, Is.EqualTo("2"));
            Assert.That(descriptor.Mutating, Is.True);
            Assert.That(descriptor.SessionScoped, Is.True);
            Assert.That(descriptor.ArgumentSchema["count"], Is.EqualTo("integer"));
            Assert.That(descriptor.Prerequisites, Is.EqualTo(new[] { "A playable map" }));
            Assert.That(run.State, Is.EqualTo("succeeded"));
            Assert.That(run.Result, Is.EqualTo("registered-result"));
            Assert.That(received, Is.EqualTo("{\"count\":3}"));
        });
    }
}
