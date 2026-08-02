using NUnit.Framework;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayApiRequestContractTests
{
    [Test]
    public void Omitted_optional_fields_receive_the_documented_direct_http_defaults()
    {
        var click = GatewayContractJson.Read<GatewayClickRequest>("{\"x\":10,\"y\":20}");
        var drag = GatewayContractJson.Read<GatewayDragRequest>(
            "{\"startX\":1,\"startY\":2,\"endX\":3,\"endY\":4}");
        var keys = GatewayContractJson.Read<GatewayKeysRequest>("{\"key\":\"Escape\"}");
        var assembly = GatewayContractJson.Read<GatewayAssemblyExecutionRequest>(
            "{\"assemblyBase64\":\"AA==\",\"entryType\":\"Probe.Entry\",\"entryMethod\":\"Execute\"}");
        var action = GatewayContractJson.Read<GatewaySemanticActionRequest>("{}");
        var defExport = GatewayContractJson.Read<GatewayDefExportApiRequest>("{}");

        Assert.Multiple(() =>
        {
            Assert.That(click.Button, Is.EqualTo("left"));
            Assert.That(click.Activate, Is.True);
            Assert.That(drag.Button, Is.EqualTo("left"));
            Assert.That(drag.DurationMs, Is.EqualTo(250));
            Assert.That(drag.Steps, Is.EqualTo(10));
            Assert.That(drag.Activate, Is.True);
            Assert.That(keys.Modifiers, Is.Empty);
            Assert.That(keys.Activate, Is.True);
            Assert.That(assembly.EntryMethod, Is.EqualTo("Execute"));
            Assert.That(assembly.RequestJson, Is.EqualTo("{}"));
            Assert.That(action.Arguments, Is.Empty);
            Assert.That(defExport.Format, Is.EqualTo("json"));
            Assert.That(defExport.DefTypes, Is.Empty);
            Assert.That(defExport.DefNames, Is.Empty);
            Assert.That(defExport.SourcePackageIds, Is.Empty);
            Assert.That(defExport.FieldNames, Is.Empty);
            Assert.That(defExport.PageSize, Is.Null);
        });
    }
}
