using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class LocalModInstallerTests
{
    [Test]
    public void Sync_StagesOnlyDeclaredFilesAndRetainsExistingCopyAsBackup()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"mods-{Guid.NewGuid():N}");
        var source = Path.Combine(root, "source");
        var mods = Path.Combine(root, "Mods");
        Directory.CreateDirectory(Path.Combine(source, "About"));
        Directory.CreateDirectory(Path.Combine(source, "1.6", "Assemblies"));
        Directory.CreateDirectory(Path.Combine(mods, "example.mod", "About"));
        File.WriteAllText(Path.Combine(source, "About", "About.xml"), "new");
        File.WriteAllText(Path.Combine(source, "1.6", "Assemblies", "Product.dll"), "dll");
        File.WriteAllText(Path.Combine(source, "1.6", "Assemblies", "Product.pdb"), "pdb");
        File.WriteAllText(Path.Combine(mods, "example.mod", "About", "About.xml"), "old");
        try
        {
            var result = LocalModInstaller.Sync(
                source,
                mods,
                "example.mod",
                ["About/About.xml", "1.6/Assemblies/Product.dll"]);

            Assert.That(File.ReadAllText(Path.Combine(result.Destination, "About", "About.xml")), Is.EqualTo("new"));
            Assert.That(File.Exists(Path.Combine(result.Destination, "1.6", "Assemblies", "Product.dll")), Is.True);
            Assert.That(File.Exists(Path.Combine(result.Destination, "1.6", "Assemblies", "Product.pdb")), Is.False);
            Assert.That(result.BackupPath, Is.Not.Null);
            Assert.That(File.ReadAllText(Path.Combine(result.BackupPath!, "About", "About.xml")), Is.EqualTo("old"));
            Assert.That(
                Path.GetFullPath(result.BackupPath!).StartsWith(
                    Path.GetFullPath(mods).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase),
                Is.False,
                "Recoverable backups must not be scanned as malformed RimWorld mods.");
            Assert.That(Directory.Exists(Path.Combine(mods, ".rimworld-modding-mcp")), Is.False);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
