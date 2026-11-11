using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class NativeXmlLoaderOperationTests
{
    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "RimWorldMods.sln"))) directory = directory.Parent;
            return directory?.FullName ?? throw new InvalidOperationException("Repository root missing.");
        }
    }
    [Test]
    public void Typed_group_selector_admits_only_the_fixed_legacy_suffix()
    {
        var command = RepositoryOperationPlanner.EndToEnd(Root,
            "brrainz.harmony|ludeon.rimworld|alpha.mod::legacy-xml", null, "English", 300, true);
        Assert.That(command.Arguments, Does.Contain("brrainz.harmony|ludeon.rimworld|alpha.mod::legacy-xml"));
    }
    [TestCase("ludeon.rimworld::legacy-xml::legacy-xml")]
    [TestCase("ludeon.rimworld::unknown")]
    [TestCase("::legacy-xml")]
    [TestCase("ludeon.rimworld::legacy-xml|alpha.mod")]
    [TestCase("ludeon.rimworld|::legacy-xml")]
    public void Typed_group_selector_rejects_ambiguous_or_unknown_modes(string group)
    {
        Assert.Throws<ArgumentException>(() => RepositoryOperationPlanner.EndToEnd(Root,
            group, null, "English", 300, true));
    }
}
