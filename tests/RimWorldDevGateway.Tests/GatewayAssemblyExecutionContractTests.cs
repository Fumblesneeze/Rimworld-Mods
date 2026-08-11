using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayAssemblyExecutionContractTests
{
    [Test]
    public void Descriptor_accepts_exact_schema_and_prerequisite_boundaries()
    {
        var schema = Enumerable.Range(0, GatewayAssemblyAutomationDescriptor.MaximumArgumentSchemaEntries)
            .ToDictionary(
                index => index == 0
                    ? new string('é', GatewayAssemblyAutomationDescriptor.MaximumArgumentSchemaKeyUtf8Bytes / 2)
                    : "key-" + index,
                index => index == 0
                    ? new string('é', GatewayAssemblyAutomationDescriptor.MaximumArgumentSchemaValueUtf8Bytes / 2)
                    : "value-" + index,
                StringComparer.Ordinal);
        var prerequisites = Enumerable.Range(0, GatewayAssemblyAutomationDescriptor.MaximumPrerequisites)
            .Select(index => index == 0
                ? new string('é', GatewayAssemblyAutomationDescriptor.MaximumPrerequisiteUtf8Bytes / 2)
                : "prerequisite-" + index)
            .ToArray();

        var descriptor = new GatewayAssemblyAutomationDescriptor(
            "boundary.fixture",
            "1",
            "Boundary fixture.",
            mutating: false,
            schema,
            prerequisites);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.ArgumentSchema, Has.Count.EqualTo(GatewayAssemblyAutomationDescriptor.MaximumArgumentSchemaEntries));
            Assert.That(descriptor.Prerequisites, Has.Count.EqualTo(GatewayAssemblyAutomationDescriptor.MaximumPrerequisites));
        });
    }

    [TestCase("schema-count")]
    [TestCase("schema-key")]
    [TestCase("schema-value")]
    [TestCase("prerequisite-count")]
    [TestCase("prerequisite-value")]
    [TestCase("metadata-total")]
    public void Descriptor_rejects_metadata_beyond_any_published_bound(string boundary)
    {
        IReadOnlyDictionary<string, string> schema = new Dictionary<string, string>
        {
            ["value"] = "string"
        };
        IEnumerable<string> prerequisites = new[] { "RimWorld is running" };
        switch (boundary)
        {
            case "schema-count":
                schema = Enumerable.Range(0, GatewayAssemblyAutomationDescriptor.MaximumArgumentSchemaEntries + 1)
                    .ToDictionary(index => "key-" + index, _ => "string", StringComparer.Ordinal);
                break;
            case "schema-key":
                schema = new Dictionary<string, string>
                {
                    [new string('é', (GatewayAssemblyAutomationDescriptor.MaximumArgumentSchemaKeyUtf8Bytes / 2) + 1)] = "string"
                };
                break;
            case "schema-value":
                schema = new Dictionary<string, string>
                {
                    ["value"] = new string('é', (GatewayAssemblyAutomationDescriptor.MaximumArgumentSchemaValueUtf8Bytes / 2) + 1)
                };
                break;
            case "prerequisite-count":
                prerequisites = Enumerable.Repeat(
                    "RimWorld is running",
                    GatewayAssemblyAutomationDescriptor.MaximumPrerequisites + 1);
                break;
            case "prerequisite-value":
                prerequisites = new[]
                {
                    new string('é', (GatewayAssemblyAutomationDescriptor.MaximumPrerequisiteUtf8Bytes / 2) + 1)
                };
                break;
            case "metadata-total":
                schema = Enumerable.Range(0, GatewayAssemblyAutomationDescriptor.MaximumArgumentSchemaEntries)
                    .ToDictionary(
                        index => "key-" + index,
                        _ => new string('x', GatewayAssemblyAutomationDescriptor.MaximumArgumentSchemaValueUtf8Bytes),
                        StringComparer.Ordinal);
                prerequisites = Enumerable.Repeat(
                    new string('x', GatewayAssemblyAutomationDescriptor.MaximumPrerequisiteUtf8Bytes),
                    GatewayAssemblyAutomationDescriptor.MaximumPrerequisites);
                break;
        }

        Assert.That(
            () => new GatewayAssemblyAutomationDescriptor(
                "oversized.fixture",
                "1",
                "Oversized fixture.",
                mutating: false,
                schema,
                prerequisites),
            Throws.TypeOf<ArgumentException>());
    }
}
