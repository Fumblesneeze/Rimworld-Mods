using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayJsonWriterTests
{
    [Test]
    public void Default_bounds_serialize_a_maximum_semantic_descriptor_page()
    {
        var descriptors = Enumerable.Range(0, 1000)
            .Select(index => new
            {
                Handle = "handle-" + index,
                Path = "Category\\Action " + index,
                Label = "Action " + index,
                Category = "Category",
                AllowedGameStates = "PlayingOnMap",
                RuntimeType = "LudeonTK.DebugActionNode",
                SourceType = "Example.DebugActions",
                Mode = "Immediate",
                Visible = true,
                Active = true,
                On = false,
                DiscoveryError = (string?)null
            })
            .ToArray();

        var bytes = GatewayJsonWriter.Write(new { Items = descriptors, PageTruncated = false });

        Assert.That(bytes.Length, Is.GreaterThan(1000));
    }
    private static readonly object?[][] ScalarCases =
    {
        new object?[] { null, "null" },
        new object?[] { "quote\" slash\\ controls\b\f\n\r\t\u0001", "\"quote\\\" slash\\\\ controls\\b\\f\\n\\r\\t\\u0001\"" },
        new object?[] { 'x', "\"x\"" },
        new object?[] { true, "true" },
        new object?[] { false, "false" },
        new object?[] { (sbyte)-8, "-8" },
        new object?[] { (byte)8, "8" },
        new object?[] { (short)-16, "-16" },
        new object?[] { (ushort)16, "16" },
        new object?[] { -32, "-32" },
        new object?[] { 32U, "32" },
        new object?[] { -64L, "-64" },
        new object?[] { 64UL, "64" },
        new object?[] { 1.25F, "1.25" },
        new object?[] { 2.5D, "2.5" },
        new object?[] { 3.75M, "3.75" },
        new object?[] { new Guid("00112233-4455-6677-8899-aabbccddeeff"), "\"00112233-4455-6677-8899-aabbccddeeff\"" },
        new object?[] { new DateTime(2026, 8, 1, 9, 10, 11, DateTimeKind.Utc), "\"2026-08-01T09:10:11.0000000Z\"" },
        new object?[] { new DateTimeOffset(2026, 8, 1, 9, 10, 11, TimeSpan.FromHours(2)), "\"2026-08-01T09:10:11.0000000+02:00\"" },
        new object?[] { GatewayState.Ready, "\"Ready\"" }
    };

    [TestCaseSource(nameof(ScalarCases))]
    public void Write_serializes_supported_scalar_values(object? value, string expected)
    {
        Assert.That(Json(value), Is.EqualTo(expected));
    }

    [Test]
    public void Write_serializes_a_public_response_shape_in_deterministic_property_order()
    {
        var bytes = GatewayJsonWriter.Write(new GatewayResponse
        {
            Zulu = true,
            Alpha = "RimWorld"
        });

        Assert.That(Encoding.UTF8.GetString(bytes), Is.EqualTo("{\"Alpha\":\"RimWorld\",\"Zulu\":true}"));
    }

    [Test]
    public void Write_serializes_dictionaries_and_sequences_in_deterministic_order()
    {
        var value = new Hashtable
        {
            ["Zulu"] = new object?[] { 3, null, "last" },
            [2] = "two",
            [10] = "ten",
            ["Alpha"] = "first"
        };

        Assert.That(
            Json(value),
            Is.EqualTo("{\"10\":\"ten\",\"2\":\"two\",\"Alpha\":\"first\",\"Zulu\":[3,null,\"last\"]}"));
    }

    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void Write_rejects_non_finite_numbers(object value)
    {
        Assert.That(
            () => GatewayJsonWriter.Write(value),
            Throws.TypeOf<GatewayJsonSerializationException>());
    }

    [Test]
    public void Write_rejects_reference_cycles_but_allows_a_shared_non_cyclic_value()
    {
        var cycle = new ArrayList();
        cycle.Add(cycle);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => GatewayJsonWriter.Write(cycle),
                Throws.TypeOf<GatewayJsonSerializationException>());

            var shared = new GatewayResponse { Alpha = "same", Zulu = true };
            Assert.That(
                Json(new object[] { shared, shared }),
                Is.EqualTo("[{\"Alpha\":\"same\",\"Zulu\":true},{\"Alpha\":\"same\",\"Zulu\":true}]"));
        });
    }

    [Test]
    public void Write_enforces_depth_and_node_budgets()
    {
        var nested = new object[] { new object[] { 1 } };

        Assert.Multiple(() =>
        {
            Assert.That(
                () => GatewayJsonWriter.Write(nested, maxDepth: 1),
                Throws.TypeOf<GatewayJsonSerializationException>());
            Assert.That(
                () => GatewayJsonWriter.Write(new object[] { 1, 2 }, maxNodes: 2),
                Throws.TypeOf<GatewayJsonSerializationException>());
            Assert.That(
                JsonWithLimits(new object[] { 1, 2 }, maxDepth: 1, maxNodes: 3),
                Is.EqualTo("[1,2]"));
            Assert.That(
                () => GatewayJsonWriter.Write(null, maxNodes: 0),
                Throws.TypeOf<GatewayJsonSerializationException>());
        });
    }

    [Test]
    public void Write_rejects_one_oversized_scalar_before_building_an_unbounded_response()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => GatewayJsonWriter.Write(new string('x', 63), maxUtf8Bytes: 64),
                Throws.TypeOf<GatewayJsonSerializationException>());
            Assert.That(
                Encoding.UTF8.GetString(GatewayJsonWriter.Write(new string('x', 62), maxUtf8Bytes: 64)),
                Has.Length.EqualTo(64));
            Assert.That(
                () => GatewayJsonWriter.Write(new string('\u20ac', 21), maxUtf8Bytes: 64),
                Throws.TypeOf<GatewayJsonSerializationException>());
        });
    }

    [Test]
    public void Write_wraps_property_getter_failures_and_rejects_indexers()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => GatewayJsonWriter.Write(new ThrowingResponse()),
                Throws.TypeOf<GatewayJsonSerializationException>()
                    .With.InnerException.TypeOf<InvalidOperationException>());
            Assert.That(
                () => GatewayJsonWriter.Write(new IndexedResponse()),
                Throws.TypeOf<GatewayJsonSerializationException>());
        });
    }

    [Test]
    public void Write_rejects_dictionary_keys_that_convert_to_the_same_property_name()
    {
        var value = new Hashtable
        {
            [1] = "integer",
            ["1"] = "string"
        };

        Assert.That(
            () => GatewayJsonWriter.Write(value),
            Throws.TypeOf<GatewayJsonSerializationException>());
    }

    private static string Json(object? value)
    {
        return Encoding.UTF8.GetString(GatewayJsonWriter.Write(value));
    }

    private static string JsonWithLimits(object? value, int maxDepth, int maxNodes)
    {
        return Encoding.UTF8.GetString(GatewayJsonWriter.Write(value, maxDepth, maxNodes));
    }

    private sealed class GatewayResponse
    {
        public bool Zulu { get; set; }

        public string Alpha { get; set; } = string.Empty;
    }

    private enum GatewayState
    {
        Ready
    }

    private sealed class ThrowingResponse
    {
        public string Value => throw new InvalidOperationException("The test getter failed.");
    }

    private sealed class IndexedResponse
    {
        public string this[int index] => index.ToString();
    }
}
