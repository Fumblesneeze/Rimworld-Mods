using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Verse;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayDefExporterTests
{
    [Test]
    public void Export_intersects_exact_filters_and_pages_in_stable_type_name_order()
    {
        var source = new StubDefSource(
            Record(typeof(ZuluDef), new ZuluDef { defName = "Zulu", label = "zulu" }, "example.target"),
            Record(typeof(AlphaDef), new AlphaDef { defName = "Beta", label = "beta" }, "example.target"),
            Record(typeof(AlphaDef), new AlphaDef { defName = "Alpha", label = "alpha" }, "example.other"),
            Record(typeof(AlphaDef), new AlphaDef { defName = "Alpha", label = "alpha target" }, "example.target"));
        var exporter = new GatewayDefExporter(source);

        var first = exporter.Export(new GatewayDefExportRequest(
            format: "json",
            defTypes: new[] { typeof(AlphaDef).FullName! },
            defNames: new[] { "Alpha", "Beta" },
            sourcePackageIds: new[] { "example.target" },
            cursor: null,
            pageSize: 1));
        var second = exporter.Export(new GatewayDefExportRequest(
            format: "json",
            defTypes: new[] { typeof(AlphaDef).FullName! },
            defNames: new[] { "Alpha", "Beta" },
            sourcePackageIds: new[] { "example.target" },
            cursor: first.NextCursor,
            pageSize: 1));

        Assert.Multiple(() =>
        {
            Assert.That(first.Items, Has.Count.EqualTo(1));
            Assert.That(first.Items[0].DefName, Is.EqualTo("Alpha"));
            Assert.That(first.Items[0].DatabaseType, Is.EqualTo(typeof(AlphaDef).FullName));
            Assert.That(first.Items[0].SourcePackageId, Is.EqualTo("example.target"));
            Assert.That(first.Items[0].SourcePackageName, Is.EqualTo("Example Mod"));
            Assert.That(first.Items[0].SourceFile, Is.EqualTo("Defs/Example.xml"));
            Assert.That(first.Truncated, Is.True);
            Assert.That(first.NextCursor, Is.Not.Null.And.Not.Empty);
            Assert.That(second.Items, Has.Count.EqualTo(1));
            Assert.That(second.Items[0].DefName, Is.EqualTo("Beta"));
            Assert.That(second.Truncated, Is.False);
            Assert.That(second.NextCursor, Is.Null);
            Assert.That(first.Serializer, Is.EqualTo("diagnostic-finalized-def-json-v1"));
            Assert.That(first.IsCanonicalSourceXml, Is.False);
        });
    }

    [Test]
    public void Export_projects_instance_fields_without_invoking_properties_or_exposing_unsaved_state()
    {
        var value = new FieldDef { defName = "FieldProbe", publicValue = 42, transient = "secret" };
        var exporter = new GatewayDefExporter(new StubDefSource(Record(typeof(FieldDef), value, "example.fields")));

        var item = exporter.Export(new GatewayDefExportRequest()).Items[0];

        Assert.Multiple(() =>
        {
            Assert.That(item.Fields["publicValue"], Is.EqualTo(42));
            Assert.That(item.Fields.ContainsKey("transient"), Is.False);
            Assert.That(item.Fields.ContainsKey("staticValue"), Is.False);
            Assert.That(item.Fields.ContainsKey(nameof(FieldDef.ThrowingProperty)), Is.False);
            Assert.That(value.PropertyReadCount, Is.Zero);
        });
    }

    [Test]
    public void Export_projects_only_requested_exact_top_level_fields_and_warns_for_missing_names()
    {
        var value = new FieldDef { defName = "FieldProbe", label = "probe label", publicValue = 42 };
        var exporter = new GatewayDefExporter(new StubDefSource(Record(typeof(FieldDef), value, "example.fields")));

        var item = exporter.Export(new GatewayDefExportRequest(
            fieldNames: new[] { "publicValue", "label", "missingXmlField" })).Items[0];

        Assert.Multiple(() =>
        {
            Assert.That(item.Fields.Keys, Is.EquivalentTo(new[] { "label", "publicValue" }));
            Assert.That(item.Fields["label"], Is.EqualTo("probe label"));
            Assert.That(item.Fields["publicValue"], Is.EqualTo(42));
            Assert.That(item.Warnings, Has.Some.Matches<GatewayDefExportWarning>(warning =>
                warning.Code == "requested_field_not_found" &&
                warning.Path == "$.missingXmlField"));
        });
    }

    [Test]
    public void Export_limits_match_the_patch_authoring_payload_policy()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GatewayDefExporter.MaximumStringLength, Is.EqualTo(64 * 1024));
            Assert.That(GatewayDefExporter.MaximumCollectionItems, Is.EqualTo(1024));
            Assert.That(GatewayDefExporter.MaximumFieldsPerObject, Is.EqualTo(512));
            Assert.That(GatewayDefExporter.MaximumUtf8BytesPerProjectedField, Is.EqualTo(1024 * 1024));
            Assert.That(GatewayDefExporter.MaximumUtf8BytesPerDef, Is.EqualTo(8 * 1024 * 1024));
            Assert.That(GatewayDefExporter.MaximumSerializedPageUtf8Bytes, Is.EqualTo(32 * 1024 * 1024));
            var maximumSegment = ((GatewayDefPagingIdentity.MaximumIdentityUtf8Bytes + 2) / 3) * 4;
            Assert.That(4 + 2 * maximumSegment, Is.EqualTo(GatewayDefExportRequest.MaximumCursorLength));
        });
    }

    [TestCase("Verse.\nThingDef", "Steel\nPatched")]
    [TestCase("Verse.\U0001F600ThingDef", "Steel\U0001F600")]
    public void Cursor_v1_round_trips_identity_text_without_a_separator_ambiguity(
        string databaseType,
        string defName)
    {
        var encoded = GatewayDefPagingIdentity.EncodeCursor(databaseType, defName);

        var decoded = GatewayDefPagingIdentity.DecodeCursor(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Does.StartWith("v1."));
            Assert.That(decoded.DatabaseType, Is.EqualTo(databaseType));
            Assert.That(decoded.DefName, Is.EqualTo(defName));
            Assert.That(encoded.Length, Is.LessThanOrEqualTo(GatewayDefExportRequest.MaximumCursorLength));
        });
    }

    [Test]
    public void Cursor_v1_accepts_the_maximum_cjk_identity_and_rejects_non_reversible_text()
    {
        var boundary = new string('\u754c', GatewayDefExportRequest.MaximumFilterValueLength);
        var encoded = GatewayDefPagingIdentity.EncodeCursor(boundary, boundary);

        var decoded = GatewayDefPagingIdentity.DecodeCursor(encoded);
        var malformed = new[]
        {
            Convert.ToBase64String(Encoding.UTF8.GetBytes("Verse.ThingDef\nSteel")),
            encoded + "=",
            "v1.not-base64!.U3RlZWw=",
            "v1." + Convert.ToBase64String(Encoding.UTF8.GetBytes("Verse.ThingDef")) + "." +
                Convert.ToBase64String(new byte[] { 0xff })
        };

        Assert.Multiple(() =>
        {
            Assert.That(decoded.DatabaseType, Is.EqualTo(boundary));
            Assert.That(decoded.DefName, Is.EqualTo(boundary));
            Assert.That(encoded.Length, Is.LessThanOrEqualTo(GatewayDefExportRequest.MaximumCursorLength));
            foreach (var value in malformed)
            {
                var exception = Assert.Throws<GatewayDefExportException>(() =>
                    new GatewayDefExportRequest(cursor: value));
                Assert.That(exception!.Code, Is.EqualTo("invalid_def_export_cursor"));
            }

            Assert.That(GatewayDefPagingIdentity.IsReversibleIdentity("bad\ud800identity"), Is.False);
            Assert.That(
                Assert.Throws<GatewayDefExportException>(() =>
                    new GatewayDefExportRequest(defNames: new[] { "bad\ud800identity" }))!.Code,
                Is.EqualTo("invalid_def_export_filter"));
        });
    }

    [Test]
    public void Export_detaches_nested_values_and_replaces_defs_and_cycles_with_compact_markers()
    {
        var referenced = new AlphaDef { defName = "Referenced" };
        var graph = new GraphNode { name = "before" };
        graph.next = graph;
        var value = new GraphDef
        {
            defName = "Graph",
            referenced = referenced,
            graph = graph
        };
        var exporter = new GatewayDefExporter(new StubDefSource(Record(typeof(GraphDef), value, "example.graph")));

        var item = exporter.Export(new GatewayDefExportRequest()).Items[0];
        graph.name = "after";
        var reference = (GatewayDefReferenceValue)item.Fields["referenced"]!;
        var projectedGraph = (IReadOnlyDictionary<string, object?>)item.Fields["graph"]!;

        Assert.Multiple(() =>
        {
            Assert.That(reference.Kind, Is.EqualTo("defReference"));
            Assert.That(reference.DefType, Is.EqualTo(typeof(AlphaDef).FullName));
            Assert.That(reference.DefName, Is.EqualTo("Referenced"));
            Assert.That(projectedGraph["name"], Is.EqualTo("before"));
            Assert.That(projectedGraph["next"], Is.TypeOf<GatewayDefRepeatedReferenceValue>());
            Assert.That(((GatewayDefRepeatedReferenceValue)projectedGraph["next"]!).FirstPath, Is.EqualTo("$.graph"));
        });
    }

    [Test]
    public void Export_does_not_invoke_virtual_type_name_or_to_string_members()
    {
        var hostileType = new HostileType(typeof(string));
        var value = new CancellationDef { defName = "HostileType", payload = hostileType };
        var exporter = new GatewayDefExporter(new StubDefSource(
            Record(typeof(CancellationDef), value, "example.hostile-type")));

        var item = exporter.Export(new GatewayDefExportRequest()).Items[0];

        Assert.Multiple(() =>
        {
            Assert.That(item.Fields["payload"], Is.EqualTo(GatewayDefSafeText.UnsupportedTypeName));
            Assert.That(hostileType.VirtualTextReadCount, Is.Zero);
        });
    }

    [Test]
    public void Export_does_not_invoke_custom_iterators_and_keeps_later_defs_exportable()
    {
        var custom = new CountingEnumerable();
        var broken = new CollectionDef { defName = "A-Custom", values = custom };
        var healthy = new CollectionDef { defName = "B-Healthy", values = new[] { 1, 2, 3 } };
        var exporter = new GatewayDefExporter(new StubDefSource(
            Record(typeof(CollectionDef), healthy, "example.collections"),
            Record(typeof(CollectionDef), broken, "example.collections")));

        var result = exporter.Export(new GatewayDefExportRequest());

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Has.Count.EqualTo(2));
            Assert.That(result.Items[0].DefName, Is.EqualTo("A-Custom"));
            Assert.That(result.Items[0].Warnings, Has.Some.Property("Code").EqualTo("unsupported_collection_type"));
            Assert.That(result.Items[0].Fields["values"], Is.TypeOf<GatewayDefProjectionErrorValue>());
            Assert.That(custom.EnumerationCount, Is.Zero);
            Assert.That(result.Items[1].DefName, Is.EqualTo("B-Healthy"));
            Assert.That((IReadOnlyList<object?>)result.Items[1].Fields["values"]!, Is.EqualTo(new object[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void Export_preserves_dictionary_keys_that_share_text_by_emitting_typed_entries()
    {
        var values = new Dictionary<object, object?>
        {
            [1] = "integer",
            ["1"] = "string"
        };
        var value = new DictionaryDef { defName = "Dictionary", values = values };
        var exporter = new GatewayDefExporter(new StubDefSource(Record(typeof(DictionaryDef), value, "example.dictionary")));

        var item = exporter.Export(new GatewayDefExportRequest()).Items[0];
        var projected = (GatewayDefDictionaryValue)item.Fields["values"]!;

        Assert.Multiple(() =>
        {
            Assert.That(projected.Kind, Is.EqualTo("dictionary"));
            Assert.That(projected.Truncated, Is.False);
            Assert.That(projected.Entries.Select(entry => (entry.KeyType, entry.Key)), Is.EqualTo(new[]
            {
                (typeof(int).FullName!, "1"),
                (typeof(string).FullName!, "1")
            }));
            Assert.That(projected.Entries.Select(entry => entry.Value), Is.EqualTo(new object[] { "integer", "string" }));
        });
    }

    [Test]
    public void Export_projects_late_declared_and_base_xml_fields_after_skipping_static_fields()
    {
        var payloadType = BuildWideProjectionProbeType();
        var payload = Activator.CreateInstance(payloadType)!;
        payloadType.GetField("zLateXmlTarget")!.SetValue(payload, 73);
        var value = new ManyFieldDef
        {
            defName = "ManyFields",
            payload = payload
        };
        var exporter = new GatewayDefExporter(new StubDefSource(Record(typeof(ManyFieldDef), value, "example.fields")));

        var item = exporter.Export(new GatewayDefExportRequest()).Items[0];
        var projected = (IReadOnlyDictionary<string, object?>)item.Fields["payload"]!;

        Assert.Multiple(() =>
        {
            Assert.That(GatewayDefExporter.MaximumFieldsPerObject, Is.GreaterThanOrEqualTo(512));
            Assert.That(projected["zLateXmlTarget"], Is.EqualTo(73));
            Assert.That(projected["baseXmlTarget"], Is.EqualTo(29));
            Assert.That(projected.ContainsKey("$projectionLimit"), Is.False);
            Assert.That(item.Warnings, Has.None.Property("Code").EqualTo("field_limit"));
        });
    }

    [Test]
    public void Export_counts_all_reflected_members_before_exclusions_and_stops_at_the_work_limit()
    {
        var payloadType = BuildReflectionWorkProbeType();
        var payload = Activator.CreateInstance(payloadType)!;
        var value = new ManyFieldDef { defName = "ReflectionWork", payload = payload };
        var exporter = new GatewayDefExporter(new StubDefSource(
            Record(typeof(ManyFieldDef), value, "example.reflection-work")));

        var item = exporter.Export(new GatewayDefExportRequest()).Items[0];
        var projected = (IReadOnlyDictionary<string, object?>)item.Fields["payload"]!;

        Assert.Multiple(() =>
        {
            Assert.That(projected.Keys, Has.None.StartsWith("aIgnoredStatic"));
            Assert.That(projected["$projectionLimit"], Is.TypeOf<GatewayDefLimitValue>());
            Assert.That(item.Warnings, Has.Some.Property("Code").EqualTo("reflected_member_work_limit"));
        });
    }

    [Test]
    public void Installed_ThingDef_hierarchy_fits_the_projection_cap_with_patch_authoring_fields()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (var current = typeof(ThingDef); current is not null && current != typeof(object); current = current.BaseType)
        {
            foreach (var field in current.GetFields(
                         BindingFlags.Instance |
                         BindingFlags.Public |
                         BindingFlags.NonPublic |
                         BindingFlags.DeclaredOnly))
            {
                if (!field.IsStatic &&
                    !CustomAttributeData.GetCustomAttributes(field).Any(attribute =>
                        string.Equals(attribute.AttributeType.FullName, "Verse.UnsavedAttribute", StringComparison.Ordinal)))
                {
                    names.Add(field.Name);
                }
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(names.Count, Is.GreaterThan(200));
            Assert.That(names.Count, Is.LessThanOrEqualTo(GatewayDefExporter.MaximumFieldsPerObject));
            Assert.That(names, Does.Contain("statBases"));
            Assert.That(names, Does.Contain("stuffProps"));
            Assert.That(names, Does.Contain("modExtensions"));
            Assert.That(names, Does.Contain("label"));
        });
    }

    private static Type BuildWideProjectionProbeType()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("GatewayDefExporterWideProjectionProbe"),
            AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("main");
        var builder = module.DefineType(
            "WideProjectionProbe",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(WideProjectionProbeBase));
        for (var index = 0; index < GatewayDefExporter.MaximumFieldsPerObject + 8; index++)
        {
            builder.DefineField(
                "ignoredStatic" + index.ToString("D4"),
                typeof(int),
                FieldAttributes.Public | FieldAttributes.Static);
        }

        for (var index = 0; index < 40; index++)
        {
            builder.DefineField(
                "xmlField" + index.ToString("D2"),
                typeof(int),
                FieldAttributes.Public);
        }

        builder.DefineField("zLateXmlTarget", typeof(int), FieldAttributes.Public);
        return builder.CreateType()!;
    }

    private static Type BuildReflectionWorkProbeType()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("GatewayDefExporterReflectionWorkProbe-" + Guid.NewGuid().ToString("N")),
            AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("main");
        var builder = module.DefineType(
            "ReflectionWorkProbe",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(object));
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        for (var index = 0; index < GatewayDefExporter.MaximumReflectedMembersExaminedPerObject + 1; index++)
        {
            builder.DefineField(
                "aIgnoredStatic" + index.ToString("D5"),
                typeof(int),
                FieldAttributes.Public | FieldAttributes.Static);
        }

        builder.DefineField("zAfterStaticWork", typeof(int), FieldAttributes.Public);
        return builder.CreateType()!;
    }

    private static object BuildLongFieldProbe(string fieldName)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("GatewayDefExporterLongFieldProbe-" + Guid.NewGuid().ToString("N")),
            AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("main");
        var builder = module.DefineType(
            "LongFieldProbe",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(object));
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        var field = builder.DefineField(fieldName, typeof(int), FieldAttributes.Public);
        var type = builder.CreateType()!;
        var value = Activator.CreateInstance(type)!;
        type.GetField(field.Name)!.SetValue(value, 17);
        return value;
    }

    private static object BuildLongNamedEnumValue(string literalName)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("GatewayDefExporterLongEnumProbe-" + Guid.NewGuid().ToString("N")),
            AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("main");
        var builder = module.DefineEnum(
            "LongEnumProbe",
            TypeAttributes.Public,
            typeof(int));
        builder.DefineLiteral(literalName, 1);
        return Enum.ToObject(builder.CreateType()!, 1);
    }

    private static (Type DatabaseType, Def Value) BuildRepresentativeWideDef()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("GatewayDefExporterRepresentativeDef-" + Guid.NewGuid().ToString("N")),
            AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("main");
        var builder = module.DefineType(
            "RepresentativeWideDef",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(Def));
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        builder.DefineField("aSmallXmlTarget", typeof(int), FieldAttributes.Public);
        builder.DefineField("bByteHeavy", typeof(object), FieldAttributes.Public);
        for (var index = 0; index < 260; index++)
        {
            builder.DefineField(
                "xmlField" + index.ToString("D3"),
                typeof(int),
                FieldAttributes.Public);
        }

        builder.DefineField("zzLateXmlTarget", typeof(int), FieldAttributes.Public);
        var type = builder.CreateType()!;
        var value = (Def)Activator.CreateInstance(type)!;
        value.defName = "RepresentativeWide";
        value.label = "representative wide Def";
        type.GetField("aSmallXmlTarget")!.SetValue(value, 11);
        type.GetField("bByteHeavy")!.SetValue(
            value,
            Enumerable.Range(0, 32)
                .Select(index => new string((char)('a' + index % 20), GatewayDefExporter.MaximumStringLength))
                .ToArray());
        type.GetField("zzLateXmlTarget")!.SetValue(value, 97);
        return (type, value);
    }

    private static (Type DatabaseType, Def Value) BuildAggregateByteHeavyDef()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("GatewayDefExporterAggregateByteDef-" + Guid.NewGuid().ToString("N")),
            AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("main");
        var builder = module.DefineType(
            "AggregateByteHeavyDef",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(Def));
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        for (var index = 0; index < 24; index++)
        {
            builder.DefineField(
                "aField" + index.ToString("D2"),
                typeof(string),
                FieldAttributes.Public);
        }

        var type = builder.CreateType()!;
        var value = (Def)Activator.CreateInstance(type)!;
        value.defName = "AggregateByteHeavy";
        for (var index = 0; index < 24; index++)
        {
            type.GetField("aField" + index.ToString("D2"))!.SetValue(
                value,
                new string('\ud800', GatewayDefExporter.MaximumStringLength));
        }

        return (type, value);
    }

    private static (Type DatabaseType, Def Value, string FieldName) BuildFieldKeyByteProbeDef()
    {
        var fieldName = new string('"', GatewayDefExporter.MaximumStringLength);
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("GatewayDefExporterFieldKeyByteProbe-" + Guid.NewGuid().ToString("N")),
            AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("main");
        var builder = module.DefineType(
            "FieldKeyByteProbeDef",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(Def));
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        builder.DefineField(fieldName, typeof(object), FieldAttributes.Public);
        var type = builder.CreateType()!;
        var value = (Def)Activator.CreateInstance(type)!;
        value.defName = "FieldKeyBytes";
        type.GetField(fieldName)!.SetValue(
            value,
            new[]
            {
                new string('\ud800', GatewayDefExporter.MaximumStringLength),
                new string('\ud800', GatewayDefExporter.MaximumStringLength),
                new string('\ud800', 22_000)
            });
        return (type, value, fieldName);
    }

    [Test]
    public void Export_bounds_nested_def_names_dictionary_keys_and_projection_paths_with_warnings()
    {
        var longText = new string('x', GatewayDefExporter.MaximumStringLength + 64);
        var value = new NestedBoundsDef
        {
            defName = "NestedBounds",
            referenced = new AlphaDef { defName = longText },
            values = new Dictionary<string, object?> { [longText] = "value" }
        };
        var exporter = new GatewayDefExporter(new StubDefSource(Record(typeof(NestedBoundsDef), value, "example.bounds")));

        var item = exporter.Export(new GatewayDefExportRequest()).Items[0];
        var reference = (GatewayDefReferenceValue)item.Fields["referenced"]!;
        var dictionary = (GatewayDefDictionaryValue)item.Fields["values"]!;

        Assert.Multiple(() =>
        {
            Assert.That(reference.DefName.Length, Is.EqualTo(GatewayDefExporter.MaximumStringLength));
            Assert.That(dictionary.Entries[0].Key.Length, Is.EqualTo(GatewayDefExporter.MaximumStringLength));
            Assert.That(item.Warnings.Count(warning => warning.Code == "string_truncated"), Is.GreaterThanOrEqualTo(2));
            Assert.That(item.Warnings.All(warning => warning.Path.Length <= 1024), Is.True);
        });
    }

    [Test]
    public void Export_counts_the_complete_top_level_field_key_and_value_against_the_byte_cap()
    {
        var (databaseType, value, fieldName) = BuildFieldKeyByteProbeDef();
        var exporter = new GatewayDefExporter(new StubDefSource(
            Record(databaseType, value, "example.field-key-bytes")));

        var item = exporter.Export(new GatewayDefExportRequest()).Items[0];
        var fieldEnvelope = new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            [fieldName] = item.Fields[fieldName]
        };
        var serialized = GatewayJsonWriter.Write(
            fieldEnvelope,
            maxDepth: GatewayDefExporter.MaximumProjectionDepth + 32,
            maxNodes: 100_000,
            maxUtf8Bytes: GatewayDefExporter.MaximumUtf8BytesPerProjectedField);

        Assert.Multiple(() =>
        {
            Assert.That(item.Fields[fieldName], Is.TypeOf<GatewayDefLimitValue>());
            Assert.That(item.Warnings, Has.Some.Property("Code").EqualTo("field_byte_limit"));
            Assert.That(serialized.Length, Is.LessThanOrEqualTo(GatewayDefExporter.MaximumUtf8BytesPerProjectedField));
        });
    }

    [Test]
    public void Export_pushes_filters_cursor_page_and_cancellation_to_the_source_and_surfaces_warnings()
    {
        var warning = new GatewayDefExportWarning(
            "Verse.ExampleDef",
            "def_database_enumeration_failed",
            "One database was unavailable.");
        var source = new RecordingPagedSource(warning);
        var exporter = new GatewayDefExporter(source);
        var first = exporter.Export(new GatewayDefExportRequest(pageSize: 1));
        using var cancellation = new CancellationTokenSource();

        var result = exporter.Export(
            new GatewayDefExportRequest(
                defTypes: new[] { typeof(AlphaDef).FullName! },
                defNames: new[] { "Beta" },
                sourcePackageIds: new[] { "example.target" },
                cursor: first.NextCursor,
                pageSize: 1),
            cancellation.Token);

        Assert.Multiple(() =>
        {
            Assert.That(source.LastQuery, Is.Not.Null);
            Assert.That(source.LastQuery!.DefTypes, Is.EqualTo(new[] { typeof(AlphaDef).FullName! }));
            Assert.That(source.LastQuery.DefNames, Is.EqualTo(new[] { "Beta" }));
            Assert.That(source.LastQuery.SourcePackageIds, Is.EqualTo(new[] { "example.target" }));
            Assert.That(source.LastQuery.AfterDatabaseType, Is.EqualTo(typeof(AlphaDef).FullName));
            Assert.That(source.LastQuery.AfterDefName, Is.EqualTo("Alpha"));
            Assert.That(source.LastQuery.Limit, Is.EqualTo(1));
            Assert.That(source.LastCancellation, Is.EqualTo(cancellation.Token));
            Assert.That(result.SourceWarnings, Has.Count.EqualTo(1));
            Assert.That(result.SourceWarnings[0].Code, Is.EqualTo("def_database_enumeration_failed"));
        });
    }

    [Test]
    public void Export_propagates_cancellation_requested_mid_array_projection()
    {
        using var cancellation = new CancellationTokenSource();
        var value = new CancellationDef
        {
            defName = "Cancellation",
            payload = new object?[]
            {
                "before",
                "trigger",
                "after"
            }
        };
        var exporter = new GatewayDefExporter(new StubDefSource(
            Record(typeof(CancellationDef), value, "example.cancellation")),
            path =>
            {
                if (string.Equals(path, "$.payload[1]", StringComparison.Ordinal))
                {
                    cancellation.Cancel();
                }
            });

        Assert.Throws<OperationCanceledException>(() =>
            exporter.Export(new GatewayDefExportRequest(), cancellation.Token));
        Assert.That(cancellation.IsCancellationRequested, Is.True);
    }

    [TestCase("list")]
    [TestCase("dictionary")]
    public void Export_propagates_cancellation_requested_inside_materialized_collections(string shape)
    {
        using var cancellation = new CancellationTokenSource();
        object payload = string.Equals(shape, "list", StringComparison.Ordinal)
            ? new List<object?> { "before", "trigger", "after" }
            : new Dictionary<string, object?>
            {
                ["before"] = "value",
                ["trigger"] = "value",
                ["zAfter"] = "value"
            };
        var value = new CancellationDef { defName = "Cancellation", payload = payload };
        var exporter = new GatewayDefExporter(new StubDefSource(
            Record(typeof(CancellationDef), value, "example.cancellation")),
            path =>
            {
                var triggerPath = string.Equals(shape, "list", StringComparison.Ordinal)
                    ? "$.payload[1]"
                    : "$.payload[trigger]";
                if (string.Equals(path, triggerPath, StringComparison.Ordinal))
                {
                    cancellation.Cancel();
                }
            });

        Assert.Throws<OperationCanceledException>(() =>
            exporter.Export(new GatewayDefExportRequest(), cancellation.Token));
        Assert.That(cancellation.IsCancellationRequested, Is.True);
    }

    [Test]
    public void Export_bounds_reflected_field_names_and_enum_text_with_projection_warnings()
    {
        var longFieldName = new string('f', GatewayDefExporter.MaximumStringLength + 64);
        var longEnumName = new string('E', GatewayDefExporter.MaximumStringLength + 64);
        var payload = BuildLongFieldProbe(longFieldName);
        var enumValue = BuildLongNamedEnumValue(longEnumName);
        var value = new DynamicBoundsDef
        {
            defName = "DynamicBounds",
            enumValue = enumValue,
            payload = payload
        };
        var exporter = new GatewayDefExporter(new StubDefSource(
            Record(typeof(DynamicBoundsDef), value, "example.dynamic-bounds")));

        var item = exporter.Export(new GatewayDefExportRequest()).Items[0];
        var projectedPayload = (IReadOnlyDictionary<string, object?>)item.Fields["payload"]!;

        Assert.Multiple(() =>
        {
            Assert.That(projectedPayload.Keys.Single().Length, Is.EqualTo(GatewayDefExporter.MaximumStringLength));
            Assert.That(((string)item.Fields["enumValue"]!).Length, Is.EqualTo(GatewayDefExporter.MaximumStringLength));
            Assert.That(item.Warnings.Count(warning => warning.Code == "string_truncated"), Is.GreaterThanOrEqualTo(2));
            Assert.That(item.Warnings.All(warning => warning.Path.Length <= 1024), Is.True);
        });
    }

    [Test]
    public void Export_marks_fixed_string_collection_and_depth_bounds()
    {
        var chain = new GraphNode { name = "root" };
        var tail = chain;
        for (var index = 0; index < GatewayDefExporter.MaximumProjectionDepth + 3; index++)
        {
            tail.next = new GraphNode { name = index.ToString() };
            tail = tail.next;
        }

        var value = new BoundsDef
        {
            defName = "Bounds",
            text = new string('x', GatewayDefExporter.MaximumStringLength + 10),
            values = Enumerable.Range(0, GatewayDefExporter.MaximumCollectionItems + 1).ToArray(),
            chain = chain
        };
        var exporter = new GatewayDefExporter(new StubDefSource(Record(typeof(BoundsDef), value, "example.bounds")));

        var item = exporter.Export(new GatewayDefExportRequest()).Items[0];
        var json = Encoding.UTF8.GetString(GatewayJsonWriter.Write(item));

        Assert.Multiple(() =>
        {
            Assert.That(((string)item.Fields["text"]!).Length, Is.EqualTo(GatewayDefExporter.MaximumStringLength));
            Assert.That((IReadOnlyList<object?>)item.Fields["values"]!, Has.Count.EqualTo(GatewayDefExporter.MaximumCollectionItems + 1));
            Assert.That(item.Warnings, Has.Some.Property("Code").EqualTo("string_truncated"));
            Assert.That(item.Warnings, Has.Some.Property("Code").EqualTo("collection_truncated"));
            Assert.That(item.Warnings, Has.Some.Property("Code").EqualTo("depth_limit"));
            Assert.That(json, Does.Contain("\"Limit\":\"depth\""));
        });
    }

    [Test]
    public void Export_caps_nodes_and_per_def_bytes_without_losing_other_page_items()
    {
        var wide = Enumerable.Range(0, 64)
            .Select(_ => (object)Enumerable.Range(0, 128).ToArray())
            .ToArray();
        var byteHeavy = Enumerable.Range(0, 32)
            .Select(index => new string((char)('a' + index % 20), GatewayDefExporter.MaximumStringLength))
            .ToArray();
        var exporter = new GatewayDefExporter(new StubDefSource(
            Record(typeof(BoundedPayloadDef), new BoundedPayloadDef { defName = "A-Nodes", payload = wide }, "example.bounds"),
            Record(typeof(BoundedPayloadDef), new BoundedPayloadDef { defName = "B-Bytes", payload = byteHeavy }, "example.bounds"),
            Record(typeof(BoundedPayloadDef), new BoundedPayloadDef { defName = "C-Healthy", payload = new[] { "ok" } }, "example.bounds")));

        var result = exporter.Export(new GatewayDefExportRequest());

        Assert.Multiple(() =>
        {
            Assert.That(result.Items[0].Warnings, Has.Some.Property("Code").EqualTo("node_limit"));
            Assert.That(result.Items[1].Warnings, Has.Some.Property("Code").EqualTo("field_byte_limit"));
            Assert.That(result.Items[1].Fields["payload"], Is.TypeOf<GatewayDefLimitValue>());
            Assert.That(result.Items[2].DefName, Is.EqualTo("C-Healthy"));
            Assert.That((IReadOnlyList<object?>)result.Items[2].Fields["payload"]!, Is.EqualTo(new object[] { "ok" }));
        });
    }

    [Test]
    public void Export_retains_a_useful_bounded_wide_def_when_one_field_is_byte_heavy()
    {
        var (databaseType, value) = BuildRepresentativeWideDef();
        var exporter = new GatewayDefExporter(new StubDefSource(
            Record(databaseType, value, "example.representative")));

        var item = exporter.Export(new GatewayDefExportRequest()).Items[0];
        var bytes = GatewayJsonWriter.Write(
            item,
            maxDepth: GatewayDefExporter.MaximumProjectionDepth + 32,
            maxNodes: 100_000,
            maxUtf8Bytes: GatewayDefExporter.MaximumUtf8BytesPerDef);

        Assert.Multiple(() =>
        {
            Assert.That(item.Fields["aSmallXmlTarget"], Is.EqualTo(11));
            Assert.That(item.Fields["zzLateXmlTarget"], Is.EqualTo(97));
            Assert.That(item.Fields.ContainsKey("label"), Is.True, "Inherited Verse.Def XML fields must remain visible.");
            Assert.That(item.Fields["bByteHeavy"], Is.TypeOf<GatewayDefLimitValue>());
            Assert.That(item.Fields.ContainsKey("$projection"), Is.False);
            Assert.That(item.Warnings, Has.Some.Property("Code").EqualTo("field_byte_limit"));
            Assert.That(bytes.Length, Is.LessThanOrEqualTo(GatewayDefExporter.MaximumUtf8BytesPerDef));
        });
    }

    [Test]
    public void Export_preserves_a_deterministic_field_prefix_when_the_whole_def_hits_its_byte_limit()
    {
        var (databaseType, value) = BuildAggregateByteHeavyDef();
        var exporter = new GatewayDefExporter(new StubDefSource(
            Record(databaseType, value, "example.aggregate-bytes")));

        var item = exporter.Export(new GatewayDefExportRequest()).Items[0];
        var bytes = GatewayJsonWriter.Write(
            item,
            maxDepth: GatewayDefExporter.MaximumProjectionDepth + 32,
            maxNodes: 100_000,
            maxUtf8Bytes: GatewayDefExporter.MaximumUtf8BytesPerDef);

        Assert.Multiple(() =>
        {
            Assert.That(item.Fields["aField00"], Is.TypeOf<string>());
            Assert.That(item.Fields["$byteLimit"], Is.TypeOf<GatewayDefLimitValue>());
            Assert.That(item.Fields.Count, Is.GreaterThan(2));
            Assert.That(item.Fields.ContainsKey("$projection"), Is.False);
            Assert.That(item.Warnings, Has.Some.Property("Code").EqualTo("per_def_byte_limit"));
            Assert.That(bytes.Length, Is.LessThanOrEqualTo(GatewayDefExporter.MaximumUtf8BytesPerDef));
        });
    }

    [Test]
    public void Exported_def_includes_worst_case_escaped_metadata_and_warnings_within_the_exact_def_cap()
    {
        var worstEscaped = new string('\ud800', GatewayDefExporter.MaximumStringLength);
        var value = new AlphaDef
        {
            defName = "WorstEscapedMetadata",
            label = worstEscaped
        };
        var record = new GatewayDefRecord(
            typeof(AlphaDef),
            value,
            worstEscaped,
            worstEscaped,
            worstEscaped);
        var exporter = new GatewayDefExporter(new StubDefSource(record));

        var item = exporter.Export(new GatewayDefExportRequest()).Items[0];
        var worstWarning = new GatewayDefExportWarning(
            new string('\ud800', 1024),
            new string('\ud800', 128),
            new string('\ud800', 1024));
        var itemWithMaximumWarnings = new GatewayDefExportItem(
            record,
            item.Fields,
            Enumerable.Repeat(worstWarning, GatewayDefExporter.MaximumWarningsPerDef).ToArray());
        var bytes = GatewayJsonWriter.Write(
            itemWithMaximumWarnings,
            maxDepth: GatewayDefExporter.MaximumProjectionDepth + 32,
            maxNodes: 100_000,
            maxUtf8Bytes: GatewayDefExporter.MaximumUtf8BytesPerDef);

        Assert.Multiple(() =>
        {
            Assert.That(item.Label, Is.EqualTo(worstEscaped));
            Assert.That(item.SourcePackageId, Is.EqualTo(worstEscaped));
            Assert.That(item.SourcePackageName, Is.EqualTo(worstEscaped));
            Assert.That(item.SourceFile, Is.EqualTo(worstEscaped));
            Assert.That(itemWithMaximumWarnings.Warnings, Has.Count.EqualTo(GatewayDefExporter.MaximumWarningsPerDef));
            Assert.That(bytes.Length, Is.LessThanOrEqualTo(GatewayDefExporter.MaximumUtf8BytesPerDef));
        });
    }

    [Test]
    public void Export_stops_before_a_result_byte_overflow_and_resumes_at_the_omitted_def()
    {
        var worstEscaped = new string('\ud800', GatewayDefExporter.MaximumStringLength);
        var records = new[]
        {
            Record(
                typeof(BoundedPayloadDef),
                new BoundedPayloadDef { defName = "A-Large", payload = new[] { worstEscaped, worstEscaped } },
                "example.page-bytes"),
            Record(
                typeof(BoundedPayloadDef),
                new BoundedPayloadDef { defName = "B-Large", payload = new[] { worstEscaped, worstEscaped } },
                "example.page-bytes")
        };
        var exporter = new GatewayDefExporter(new StubDefSource(records));
        const int resultBudget = 1024 * 1024;

        var first = exporter.Export(
            new GatewayDefExportRequest(pageSize: 2),
            resultBudget,
            CancellationToken.None);
        var second = exporter.Export(
            new GatewayDefExportRequest(pageSize: 2, cursor: first.NextCursor),
            resultBudget,
            CancellationToken.None);
        var firstBytes = GatewayJsonWriter.Write(
            first,
            maxDepth: 64,
            maxNodes: 500_000,
            maxUtf8Bytes: resultBudget);

        Assert.Multiple(() =>
        {
            Assert.That(first.Items.Select(item => item.DefName), Is.EqualTo(new[] { "A-Large" }));
            Assert.That(first.Truncated, Is.True);
            Assert.That(first.NextCursor, Is.Not.Null.And.Not.Empty);
            Assert.That(second.Items.Select(item => item.DefName), Is.EqualTo(new[] { "B-Large" }));
            Assert.That(second.Truncated, Is.False);
            Assert.That(second.NextCursor, Is.Null);
            Assert.That(firstBytes.Length, Is.LessThanOrEqualTo(resultBudget));
        });
    }

    [Test]
    public void Export_rejects_xml_explicitly_before_reading_live_defs()
    {
        var source = new CountingDefSource();
        var exporter = new GatewayDefExporter(source);

        var exception = Assert.Throws<GatewayDefExportException>(() =>
            exporter.Export(new GatewayDefExportRequest(format: "xml")));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo("unsupported_def_export_format"));
            Assert.That(exception.Message, Does.Contain("JSON"));
            Assert.That(exception.Message, Does.Contain("not canonical"));
            Assert.That(source.CaptureCount, Is.Zero);
        });
    }

    [Test]
    public void Export_applies_page_limit_before_projecting_omitted_defs()
    {
        var omittedCollection = new CountingEnumerable();
        var exporter = new GatewayDefExporter(new StubDefSource(
            Record(typeof(CollectionDef), new CollectionDef { defName = "A-First", values = new[] { 1 } }, "example.page"),
            Record(typeof(CollectionDef), new CollectionDef { defName = "B-Omitted", values = omittedCollection }, "example.page")));

        var result = exporter.Export(new GatewayDefExportRequest(pageSize: 1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.That(result.Items[0].DefName, Is.EqualTo("A-First"));
            Assert.That(result.Truncated, Is.True);
            Assert.That(omittedCollection.EnumerationCount, Is.Zero);
        });
    }

    [Test]
    public void Export_rejects_requested_page_size_above_the_fixed_maximum()
    {
        var records = Enumerable.Range(0, GatewayDefExporter.MaximumPageSize + 4)
            .Select(index => Record(
                typeof(AlphaDef),
                new AlphaDef { defName = "Def-" + index.ToString("D3") },
                "example.page"))
            .ToArray();
        var exporter = new GatewayDefExporter(new StubDefSource(records));

        var exception = Assert.Throws<GatewayDefExportException>(() =>
            exporter.Export(new GatewayDefExportRequest(pageSize: int.MaxValue)));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo("invalid_def_export_page_size"));
        });
    }

    [Test]
    public void Exported_maximum_page_stays_below_the_fixed_transport_bound()
    {
        var records = Enumerable.Range(0, GatewayDefExporter.MaximumPageSize)
            .Select(index => Record(
                typeof(BoundedPayloadDef),
                new BoundedPayloadDef
                {
                    defName = "Payload-" + index.ToString("D3"),
                    payload = Enumerable.Range(0, 12)
                        .Select(character => new string((char)('a' + character), GatewayDefExporter.MaximumStringLength))
                        .ToArray()
                },
                "example.transport"))
            .ToArray();
        var exporter = new GatewayDefExporter(new StubDefSource(records));

        var result = exporter.Export(new GatewayDefExportRequest(pageSize: GatewayDefExporter.MaximumPageSize));
        var bytes = GatewayJsonWriter.Write(
            result,
            maxDepth: 64,
            maxNodes: 250_000,
            maxUtf8Bytes: GatewayDefExporter.MaximumSerializedPageUtf8Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Has.Count.EqualTo(GatewayDefExporter.MaximumPageSize));
            Assert.That(bytes.Length, Is.LessThanOrEqualTo(GatewayDefExporter.MaximumSerializedPageUtf8Bytes));
        });
    }

    private static GatewayDefRecord Record(Type databaseType, Def value, string packageId) =>
        new GatewayDefRecord(databaseType, value, packageId, "Example Mod", "Defs/Example.xml");

    private sealed class StubDefSource : IGatewayDefSource
    {
        private readonly IReadOnlyList<GatewayDefRecord> records;

        public StubDefSource(params GatewayDefRecord[] records)
        {
            this.records = records;
        }

        public GatewayDefSourcePage CapturePage(
            GatewayDefSourceQuery query,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var selected = records
                .Where(record => query.DefTypes.Count == 0 || query.DefTypes.Contains(record.DatabaseType.FullName!, StringComparer.Ordinal))
                .Where(record => query.DefNames.Count == 0 || query.DefNames.Contains(record.Value.defName, StringComparer.Ordinal))
                .Where(record => query.SourcePackageIds.Count == 0 || query.SourcePackageIds.Contains(record.SourcePackageId!, StringComparer.Ordinal))
                .OrderBy(record => record.DatabaseType.FullName, StringComparer.Ordinal)
                .ThenBy(record => record.Value.defName, StringComparer.Ordinal)
                .Where(record => query.AfterDatabaseType is null ||
                    StringComparer.Ordinal.Compare(record.DatabaseType.FullName, query.AfterDatabaseType) > 0 ||
                    string.Equals(record.DatabaseType.FullName, query.AfterDatabaseType, StringComparison.Ordinal) &&
                    StringComparer.Ordinal.Compare(record.Value.defName, query.AfterDefName) > 0)
                .Take(query.Limit + 1)
                .ToArray();
            return new GatewayDefSourcePage(
                selected.Take(query.Limit),
                selected.Length > query.Limit);
        }
    }

    private sealed class CountingDefSource : IGatewayDefSource
    {
        public int CaptureCount { get; private set; }

        public GatewayDefSourcePage CapturePage(
            GatewayDefSourceQuery query,
            CancellationToken cancellationToken)
        {
            CaptureCount++;
            return new GatewayDefSourcePage(Array.Empty<GatewayDefRecord>(), truncated: false);
        }
    }

    private sealed class RecordingPagedSource : IGatewayDefSource
    {
        private readonly GatewayDefExportWarning warning;
        private int calls;

        public RecordingPagedSource(GatewayDefExportWarning warning)
        {
            this.warning = warning;
        }

        public GatewayDefSourceQuery? LastQuery { get; private set; }

        public CancellationToken LastCancellation { get; private set; }

        public GatewayDefSourcePage CapturePage(GatewayDefSourceQuery query, CancellationToken cancellationToken)
        {
            LastQuery = query;
            LastCancellation = cancellationToken;
            calls++;
            var record = calls == 1
                ? Record(typeof(AlphaDef), new AlphaDef { defName = "Alpha" }, "example.target")
                : Record(typeof(AlphaDef), new AlphaDef { defName = "Beta" }, "example.target");
            return new GatewayDefSourcePage(
                new[] { record },
                truncated: calls == 1,
                sourceWarnings: calls == 1 ? Array.Empty<GatewayDefExportWarning>() : new[] { warning });
        }
    }

    private sealed class AlphaDef : Def
    {
        public int value = 1;
    }

    private sealed class ZuluDef : Def
    {
        public int value = 2;
    }

    private sealed class FieldDef : Def
    {
        public static int staticValue = 7;
        public int publicValue;

        [Unsaved(false)]
        public string? transient;

        [Unsaved(false)]
        public int PropertyReadCount;

        public string ThrowingProperty
        {
            get
            {
                PropertyReadCount++;
                throw new InvalidOperationException("Property getters must not run.");
            }
        }
    }

    private sealed class GraphDef : Def
    {
        public Def? referenced;
        public GraphNode? graph;
    }

    private sealed class GraphNode
    {
        public string name = string.Empty;
        public GraphNode? next;
    }

    private sealed class CollectionDef : Def
    {
        public IEnumerable? values;
    }

    private sealed class DictionaryDef : Def
    {
        public IDictionary? values;
    }

    private sealed class NestedBoundsDef : Def
    {
        public Def? referenced;
        public IDictionary? values;
    }

    private sealed class CountingEnumerable : IEnumerable
    {
        public int EnumerationCount { get; private set; }

        public IEnumerator GetEnumerator()
        {
            EnumerationCount++;
            return Array.Empty<object>().GetEnumerator();
        }
    }

    private sealed class BoundsDef : Def
    {
        public string text = string.Empty;
        public IReadOnlyList<int> values = Array.Empty<int>();
        public GraphNode? chain;
    }

    private sealed class BoundedPayloadDef : Def
    {
        public IEnumerable? payload;
    }

    private sealed class CancellationDef : Def
    {
        public object? payload;
    }

    private sealed class DynamicBoundsDef : Def
    {
        public object? enumValue;
        public object? payload;
    }

    private sealed class HostileType : TypeDelegator
    {
        public HostileType(Type delegatingType)
            : base(delegatingType)
        {
        }

        public int VirtualTextReadCount { get; private set; }

        public override string? FullName
        {
            get
            {
                VirtualTextReadCount++;
                throw new InvalidOperationException("FullName must not be invoked.");
            }
        }

        public override string Name
        {
            get
            {
                VirtualTextReadCount++;
                throw new InvalidOperationException("Name must not be invoked.");
            }
        }

        public override string ToString()
        {
            VirtualTextReadCount++;
            throw new InvalidOperationException("ToString must not be invoked.");
        }
    }

    private sealed class ManyFieldDef : Def
    {
        public object? payload;
    }

    public class WideProjectionProbeBase
    {
        public int baseXmlTarget = 29;
    }
}
