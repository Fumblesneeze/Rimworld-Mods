using System.Xml;
using Megamod.Features.Diagnostics;
using NUnit.Framework;

namespace Megamod.Unit.Features.Diagnostics;

[TestFixture]
public sealed class DefinitionContextCaptureTests
{
    [TestCase("<ThingDef><nested><defName>Wrong</defName></nested></ThingDef>", null, "ThingDef")]
    [TestCase("<ThingDef><defName><nested>Wrong</nested></defName></ThingDef>", null, "ThingDef")]
    [TestCase("<ThingDef><defName><![CDATA[Actual]]></defName></ThingDef>", "Actual", "ThingDef")]
    public void MissingOrStructuredNamesStayUnknownAndGeneratedPathsStayRelative(string xmlText, string? expectedName, string expectedKind)
    {
        var xml = new XmlDocument();
        xml.LoadXml(xmlText);
        var context = DefinitionContextCapture.Read(xml.DocumentElement, new DefinitionSource(null, null, null, "", "generated.xml"));
        Assert.That(context.DefinitionName, Is.EqualTo(expectedName));
        Assert.That(context.DeclaredKind, Is.EqualTo(expectedKind));
        Assert.That(context.SourceFilePath, Is.EqualTo("generated.xml"));
        Assert.That(context.PackageId, Is.Null);
    }

    [Test]
    public void MetadataScanStopsWithoutInventingANestedOrUnboundedName()
    {
        var xml = new XmlDocument();
        xml.LoadXml("<ThingDef>" + string.Concat(Enumerable.Repeat("<other/>", 4096)) + "<defName>BeyondBound</defName></ThingDef>");
        var context = DefinitionContextCapture.Read(xml.DocumentElement, new DefinitionSource(null, null, null, null, null));
        Assert.That(context.DefinitionName, Is.Null);
        Assert.That(context.DeclaredKind, Is.EqualTo("ThingDef"));
    }
    [Test]
    public void OneUnreadableFieldDoesNotDiscardOtherKnownFieldsOrThrow()
    {
        var node = new UnreadableName();
        var name = node.OwnerDocument!.CreateElement("defName");
        name.InnerText = "Actual";
        node.AppendChild(name);
        int failures = 0;
        var context = DefinitionContextCapture.Read(node, new DefinitionSource(null, "known.mod", null, null, "generated.xml"), _ => failures++);
        Assert.That(context, Is.EqualTo(new DefinitionContext("Actual", null, null, "known.mod", null, "generated.xml")));
        Assert.That(failures, Is.EqualTo(1));
    }
    private sealed class UnreadableName : XmlElement
    {
        public UnreadableName() : base("", "ThingDef", "", new XmlDocument()) { }
        public override string Name => throw new InvalidOperationException("Metadata unavailable");
    }

    [Test]
    public void CapturesOnlyTheCurrentDirectDefinitionAndKnownSource()
    {
        var xml = new XmlDocument();
        xml.LoadXml("<ThingDef Class='Example.Kind'><nested><defName>Wrong</defName></nested><defName>Actual</defName></ThingDef>");
        var context = DefinitionContextCapture.Read(xml.DocumentElement, new DefinitionSource("Mod", "example.mod", "C:/Mod", "C:/Mod/Defs", "a.xml"));
        Assert.That(context, Is.EqualTo(new DefinitionContext("Actual", "Example.Kind", "Mod", "example.mod", "C:/Mod", Path.Combine("C:/Mod/Defs", "a.xml"))));
    }
}
