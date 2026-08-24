using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class RepositoryOperationTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"operations-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_root, "scripts"));
        Directory.CreateDirectory(Path.Combine(_root, "mods", "Example"));
        Directory.CreateDirectory(Path.Combine(_root, "openspec"));
        File.WriteAllText(Path.Combine(_root, "AGENTS.md"), "fixture");
        File.WriteAllText(Path.Combine(_root, "scripts", "Invoke-Tests.ps1"), "# adapter");
        File.WriteAllText(Path.Combine(_root, "scripts", "Invoke-RimWorldEndToEndTests.ps1"), "# adapter");
        File.WriteAllText(Path.Combine(_root, "scripts", "Invoke-GatewaySmoke.ps1"), "# adapter");
        File.WriteAllText(Path.Combine(_root, "mods", "Example", "Example.csproj"), "<Project />");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Test]
    public void TestPlan_UsesExactSuiteFilterAndJsonEvidence()
    {
        var plan = RepositoryOperationPlanner.TestRun(
            _root,
            "GuestBedGizmo.Harmony",
            "FullyQualifiedName=Example.Tests.One",
            "Release");

        Assert.That(plan.FileName, Is.EqualTo("pwsh"));
        Assert.That(plan.Arguments, Does.Contain("-Suite"));
        Assert.That(plan.Arguments, Does.Contain("GuestBedGizmo.Harmony"));
        Assert.That(plan.Arguments, Does.Contain("-TestFilter"));
        Assert.That(plan.Arguments, Does.Contain("FullyQualifiedName=Example.Tests.One"));
        Assert.That(plan.Arguments.TakeLast(2), Is.EqualTo(new[] { "-Output", "json" }));
    }

    [Test]
    public async Task OpenSpecOperation_ExecutesThroughTheWindowsCommandShim()
    {
        if (!OperatingSystem.IsWindows()) Assert.Ignore("Windows command-shim regression.");
        var root = TestRepository.FindRoot();
        var result = await RepositoryOperations.ExecuteAsync(
            RepositoryOperationPlanner.OpenSpecValidate(root), CancellationToken.None);

        Assert.That(result.ExitCode, Is.Zero, result.StandardError + result.StandardOutput);
        Assert.That(result.StandardOutput, Does.Contain("0 failed"));
    }

    [TestCase("Guest Bed")]
    [TestCase("../outside")]
    [TestCase("x; Remove-Item C:\\")]
    public void TestPlan_RejectsNonLiteralSuite(string suite)
    {
        Assert.Throws<ArgumentException>(() => RepositoryOperationPlanner.TestRun(_root, suite, null, "Release"));
    }

    [Test]
    public void EndToEndPlan_RequiresExactlyOneBoundedSelector()
    {
        Assert.Throws<ArgumentException>(() => RepositoryOperationPlanner.EndToEnd(
            _root, null, null, "English", 300, dryRun: false));
        Assert.Throws<ArgumentException>(() => RepositoryOperationPlanner.EndToEnd(
            _root, "group", "test", "English", 300, dryRun: false));

        var plan = RepositoryOperationPlanner.EndToEnd(
            _root, null, "guest-bed-owner-menu", "English", 300, dryRun: true);
        Assert.That(plan.Arguments, Does.Contain("-TestId"));
        Assert.That(plan.Arguments, Does.Contain("-DryRun"));

        var packageGroup = RepositoryOperationPlanner.EndToEnd(
            _root,
            "brrainz.harmony|ludeon.rimworld|ludeon.rimworld.ideology|orion.hospitality|fumblesneeze.guestbedgizmo",
            null,
            "English",
            300,
            dryRun: true);
        Assert.That(packageGroup.Arguments, Does.Contain("-GroupId"));
    }

    [Test]
    public void GatewayRunPlan_UsesOwnedArtifactsCompletionAndLeasePaths()
    {
        var runRoot = Path.Combine(_root, "artifacts", "McpRuns", "run-1");
        var plan = RepositoryOperationPlanner.GatewayRun(
            _root,
            runRoot,
            ["brrainz.harmony", "fumblesneeze.example"],
            ["mods/Example/Example.csproj"],
            900);

        Assert.That(plan.Arguments, Does.Contain("-InteractiveCompletionFile"));
        Assert.That(plan.Arguments, Does.Contain(Path.Combine(runRoot, "complete.signal")));
        Assert.That(plan.Arguments, Does.Contain("-ProcessLeaseFile"));
        Assert.That(plan.Arguments, Does.Contain(Path.Combine(runRoot, "game-process.json")));
        Assert.That(plan.Arguments, Does.Contain("-AdditionalModIdsFile"));
        Assert.That(
            File.ReadAllLines(Path.Combine(runRoot, "additional-mod-ids.txt")),
            Is.EqualTo(new[] { "brrainz.harmony", "fumblesneeze.example" }));
    }
}
