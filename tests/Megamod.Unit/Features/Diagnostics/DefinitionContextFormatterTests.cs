using Megamod.Features.Diagnostics;
using NUnit.Framework;

namespace Megamod.Unit.Features.Diagnostics;

[TestFixture]
public sealed class DefinitionContextFormatterTests
{
    [TestCase("\n", @"\n", @"\\n")]
    [TestCase("\t", @"\t", @"\\t")]
    [TestCase("\u2028", @"\u2028", @"\\u2028")]
    public void LiteralEscapeTextRemainsDistinctFromAnActualMetadataControl(
        string control, string controlDisplay, string literalDisplay)
    {
        const string original = "native literal \\n and actual\nnewline";
        var actualContext = new DefinitionContext("First" + control + "Second", "ThingDef", null, null, null, null);
        var literalContext = actualContext with { DefinitionName = "First" + controlDisplay + "Second" };

        string actual = DefinitionContextFormatter.Append(original, actualContext);
        string literal = DefinitionContextFormatter.Append(original, literalContext);

        Assert.That(literal, Is.Not.EqualTo(actual), "Distinct metadata must not become identical native repeat text.");
        Assert.That(actual, Does.StartWith(original + "\n\n[Def Error]"));
        Assert.That(literal, Does.StartWith(original + "\n\n[Def Error]"));
        Assert.That(actual, Does.Contain("Definition: First" + controlDisplay + "Second"));
        Assert.That(literal, Does.Contain("Definition: First" + literalDisplay + "Second"));
    }

    [Test]
    public void InvalidPathCannotHideTheOriginalOrOtherKnownFields()
    {
        string source = Path.GetPathRoot(Environment.CurrentDirectory)! + "bad\0file.xml";
        var context = new DefinitionContext("Example", "ThingDef", "Source mod", "fixture.source",
            Environment.CurrentDirectory, source);

        string projected = DefinitionContextFormatter.Append("native error", context);

        Assert.That(projected, Does.StartWith("native error\n\n[Def Error]"));
        Assert.That(projected, Does.Contain("Definition: Example").And.Contain("Source mod: Source mod"));
        Assert.That(projected, Does.Contain("XML file (unresolved path): " + source.Replace("\\", "\\\\").Replace("\0", "\\u0000")));
    }

    [Test]
    public void MetadataControlsCannotCreateNewContextFieldsOrChangeTheOriginal()
    {
        const string original = "native\nwarning";
        var context = new DefinitionContext("Example\nPackage: invented", "ThingDef", "mod\r\tname\u2028next",
            "fixture.source", null, "folder\nFake.xml");

        string projected = DefinitionContextFormatter.Append(original, context);

        Assert.That(projected, Does.StartWith(original + "\n\n[Def Error]"));
        Assert.That(projected, Does.Contain("Definition: Example\\nPackage: invented"));
        Assert.That(projected, Does.Contain("Source mod: mod\\r\\tname\\u2028next"));
        Assert.That(projected, Does.Contain("XML file (unresolved path): folder\\nFake.xml"));
        Assert.That(projected, Does.Not.Contain("\nPackage: invented").And.Not.Contain("\u2028"));
    }

    [Test]
    public void RelativeSourceWithoutAnAbsolutePathDoesNotInventWorkingDirectoryProvenance()
    {
        var context = new DefinitionContext("Generated", "ThingDef", null, null, null, "Generated.xml");

        string projected = DefinitionContextFormatter.Append("native warning", context);

        Assert.That(projected, Does.Contain("XML file (unresolved path): Generated.xml"));
        Assert.That(projected, Does.Not.Contain(Environment.CurrentDirectory));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UnknownFieldsRemainIndependentOfKnownDefinitionAndFile(bool knownFile)
    {
        string? file = knownFile ? Path.GetFullPath("Generated.xml") : null;
        var context = new DefinitionContext("Generated", null, null, null, null, file);

        string projected = DefinitionContextFormatter.Append("native warning", context);

        Assert.That(projected, Does.Contain("Definition: Generated").And.Contain("Declared kind: unknown"));
        Assert.That(projected, Does.Contain("Source mod: unknown").And.Contain("Package: unknown"));
        Assert.That(projected, Does.Contain(knownFile ? "XML file (external path): " + file!.Replace("\\", "\\\\") : "XML file: unknown"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PathsOutsideTheModRemainExplicitlyExternal(bool traversesParent)
    {
        string root = Path.GetFullPath(Path.Combine("fixtures", "source-mod"));
        string source = traversesParent
            ? Path.Combine(root, "Defs", "..", "..", "Other.xml")
            : Path.Combine(root + "-sibling", "Other.xml");
        var context = new DefinitionContext("Other", "ThingDef", "Source mod", "fixture.source", root, source);

        string projected = DefinitionContextFormatter.Append("native warning", context);

        Assert.That(projected, Does.Contain("XML file (external path): " + Path.GetFullPath(source).Replace("\\", "\\\\")));
        Assert.That(projected, Does.Not.Contain("XML file (relative to source mod)"));
    }

    [Test]
    public void OriginalDiagnosticRemainsIntactBesideCurrentRelativeSourceContext()
    {
        string original = "<color=red>native warning</color>\n" + new string('x', 2048);
        string root = Path.GetFullPath(Path.Combine("fixtures", "source-mod"));
        var context = new DefinitionContext("Example", "ThingDef", "Source mod", "fixture.source",
            root, Path.Combine(root, "Defs", "Example.xml"));

        string projected = DefinitionContextFormatter.Append(original, context);

        Assert.That(projected, Does.StartWith(original + "\n\n[Def Error]"));
        Assert.That(projected, Does.Contain("Definition: Example").And.Contain("Declared kind: ThingDef"));
        Assert.That(projected, Does.Contain("Source mod: Source mod").And.Contain("Package: fixture.source"));
        Assert.That(projected, Does.Contain("XML file (relative to source mod): " + Path.Combine("Defs", "Example.xml").Replace("\\", "\\\\")));
    }
}
