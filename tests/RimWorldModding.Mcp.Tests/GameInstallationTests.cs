using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class GameInstallationTests
{
    [Test]
    public void Any_patch_and_revision_within_the_same_major_minor_is_accepted()
    {
        Arrange("1.6.4871 rev590", "RimWorldWin64.exe", "RimWorldWin64_Data/Managed", "UnityEngine.CoreModule.dll");
        Assert.That(() => GameInstallation.ForExecutable(Path.Combine(root, "RimWorldWin64.exe"), "1.6.9999 rev12345"), Throws.Nothing);
    }

    private string root = null!;

    [SetUp]
    public void SetUp() => root = Directory.CreateDirectory(Path.Combine(
        TestContext.CurrentContext.WorkDirectory, "runtime-fixture-" + Guid.NewGuid().ToString("N"))).FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(root, true);

    [TestCase("1.6.4871 rev1", "1.6", "RimWorldWin64.exe", "RimWorldWin64_Data/Managed", "UnityEngine.CoreModule.dll", "net480")]
    [TestCase("1.7.4984 rev1242", "1.7", "RimWorld.exe", ".", "UnityStubs.dll", "net10.0")]
    public void SelectsCompleteRuntimeWithoutLoadingGameAssemblies(
        string version, string major, string executable, string managed, string shim, string framework)
    {
        Arrange(version, executable, managed, shim);
        var installation = GameInstallation.Inspect(root);
        Assert.Multiple(() =>
        {
            Assert.That(installation.Version, Is.EqualTo(version));
            Assert.That(installation.MajorVersion, Is.EqualTo(major));
            Assert.That(installation.ExecutablePath, Is.EqualTo(Path.Combine(root, executable)));
            Assert.That(installation.ManagedPath, Is.EqualTo(Path.GetFullPath(Path.Combine(root, managed))));
            Assert.That(installation.TargetFramework, Is.EqualTo(framework));
        });
    }

    [TestCase("1.8.1")]
    [TestCase("1.70.1")]
    [TestCase("invalid")]
    public void RejectsUnsupportedOrMalformedVersions(string version)
    {
        Arrange(version, "RimWorld.exe", ".", "UnityStubs.dll");
        Assert.That(() => GameInstallation.Inspect(root), Throws.ArgumentException);
    }

    [Test]
    public void SessionCompilationRejectsDifferentMajorMinorOrExecutable()
    {
        Arrange("1.7.4984 rev1242", "RimWorld.exe", ".", "UnityStubs.dll");
        Assert.That(GameInstallation.ForExecutable(Path.Combine(root, "RimWorld.exe"), "1.7.4984 rev1242").Root,
            Is.EqualTo(root));
        Assert.Throws<ArgumentException>(() => GameInstallation.ForExecutable(Path.Combine(root, "RimWorld.exe"), "1.6.9999 rev123"));
        Assert.Throws<ArgumentException>(() => GameInstallation.ForExecutable(Path.Combine(root, "Other.exe"), "1.7.4984 rev1242"));
    }

    [Test]
    public async Task TypedOperationInspectsAnExplicitInstallationWithoutStartingIt()
    {
        Arrange("1.7.4984 rev1242", "RimWorld.exe", ".", "UnityStubs.dll");
        var registry = OperationRegistry.CreateDefault(TestRepository.FindRoot());
        var result = await registry.InvokeAsync("game_installation_inspect",
            System.Text.Json.JsonSerializer.SerializeToElement(new { gameRoot = root }), CancellationToken.None);
        Assert.That(result, Is.TypeOf<GameInstallation>());
        Assert.That(((GameInstallation)result).Root, Is.EqualTo(root));
        Assert.That(OperationJson.Serialize(result), Does.Contain("net10.0"));
    }

    [TestCase("RimWorld.exe")]
    [TestCase("Assembly-CSharp.dll")]
    [TestCase("UnityStubs.dll")]
    public void RejectsIncompleteBetaInsteadOfFallingBackToStaleLegacyFiles(string missing)
    {
        Arrange("1.7.4984 rev1242", "RimWorld.exe", ".", "UnityStubs.dll");
        File.Delete(Path.Combine(root, missing));
        Assert.That(() => GameInstallation.Inspect(root), Throws.ArgumentException.With.Message.Contains(missing));
    }

    private void Arrange(string version, string executable, string managed, string shim)
    {
        File.WriteAllText(Path.Combine(root, "Version.txt"), version);
        File.WriteAllBytes(Path.Combine(root, executable), [0]);
        var directory = Directory.CreateDirectory(Path.Combine(root, managed)).FullName;
        File.WriteAllBytes(Path.Combine(directory, "Assembly-CSharp.dll"), [0]);
        File.WriteAllBytes(Path.Combine(directory, shim), [0]);
    }
}
