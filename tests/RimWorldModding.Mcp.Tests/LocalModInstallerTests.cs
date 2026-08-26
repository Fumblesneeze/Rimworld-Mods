using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class LocalModInstallerTests
{
    [Test]
    public void Sync_StagesOnlyDeclaredFilesAndReplacesExistingCopyWithoutCreatingABackup()
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
            Assert.That(result.BackupPath, Is.Null);
            Assert.That(Directory.Exists(Path.Combine(mods, ".rimworld-modding-mcp")), Is.False);
            Assert.That(
                Directory.GetDirectories(Path.GetDirectoryName(mods)!, ".rimworld-modding-mcp", SearchOption.TopDirectoryOnly),
                Is.Empty,
                "A normal local build sync must not create a recovery/backup tree beside Mods.");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task BuildAndInstall_CopiesAfterSuccessfulBuildByDefault()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"build-install-{Guid.NewGuid():N}");
        var package = Path.Combine(root, "package");
        var mods = Path.Combine(root, "Mods");
        Directory.CreateDirectory(Path.Combine(package, "About"));
        Directory.CreateDirectory(mods);
        File.WriteAllText(Path.Combine(package, "About", "About.xml"), "built");
        try
        {
            var command = new AdapterCommand("mod_build", "fake-build", [], root, TimeSpan.FromSeconds(1));
            var result = await RepositoryOperations.BuildAndInstallAsync(
                command,
                package,
                "example.mod",
                ["About/About.xml"],
                mods,
                CancellationToken.None,
                (_, _) => Task.FromResult(new AdapterOperationResult(
                    "mod_build", 0, 0.01, "built", "", null)));

            Assert.That(result.Build.ExitCode, Is.Zero);
            Assert.That(File.ReadAllText(Path.Combine(result.Installation.Destination, "About", "About.xml")), Is.EqualTo("built"));
            Assert.That(result.Installation.BackupPath, Is.Null);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task BuildAndInstall_DoesNotReplacePackageWhenBuildFails()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"build-install-failed-{Guid.NewGuid():N}");
        var package = Path.Combine(root, "package");
        var mods = Path.Combine(root, "Mods");
        Directory.CreateDirectory(Path.Combine(package, "About"));
        Directory.CreateDirectory(Path.Combine(mods, "example.mod", "About"));
        File.WriteAllText(Path.Combine(package, "About", "About.xml"), "new-but-not-installed");
        File.WriteAllText(Path.Combine(mods, "example.mod", "About", "About.xml"), "existing");
        try
        {
            var command = new AdapterCommand("mod_build", "fake-build", [], root, TimeSpan.FromSeconds(1));
            var error = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await RepositoryOperations.BuildAndInstallAsync(
                    command,
                    package,
                    "example.mod",
                    ["About/About.xml"],
                    mods,
                    CancellationToken.None,
                    (_, _) => Task.FromResult(new AdapterOperationResult(
                        "mod_build", 1, 0.01, "", "compile failed", null))));

            Assert.That(error!.Message, Does.Contain("not synchronized"));
            Assert.That(File.ReadAllText(Path.Combine(mods, "example.mod", "About", "About.xml")), Is.EqualTo("existing"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
