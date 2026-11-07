using NUnit.Framework;
using RimWorldDevGateway.EndToEndHost;
using System.Text.Json;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class RuntimeBundleTests
{
    [TestCase("1.7.4984 rev1242", "1.6", false)]
    [TestCase("1.7.invalid", "", false)]
    [TestCase("1.7.4984 rev1242", "", true)]
    [TestCase("1.6.4871 rev1", "", true)]
    public void BuildImportsRejectMalformedAndConflictingVersions(string version, string requested, bool valid)
    {
        var root = Path.Combine(Path.GetTempPath(), "runtime-import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "Version.txt"), version);
            File.WriteAllBytes(Path.Combine(root, "Assembly-CSharp.dll"), Array.Empty<byte>());
            File.WriteAllBytes(Path.Combine(root, "UnityStubs.dll"), Array.Empty<byte>());
            Directory.CreateDirectory(Path.Combine(root, "RimWorldWin64_Data/Managed"));
            File.WriteAllBytes(Path.Combine(root, "RimWorldWin64_Data/Managed/Assembly-CSharp.dll"), Array.Empty<byte>());
            var imports = Path.Combine(TestRepository.FindRoot(), "tools/RimWorld.Build");
            var project = Path.Combine(root, "Probe.proj");
            File.WriteAllText(project, $"<Project><PropertyGroup><RimWorldPath>{root}</RimWorldPath>" +
                $"<RimWorldVersion>{requested}</RimWorldVersion></PropertyGroup>" +
                $"<Import Project=\"{imports}/VersionedGame.props\"/>" +
                "<PropertyGroup><RimWorldVersion Condition=\"'$(RimWorldVersion)' == ''\">1.7</RimWorldVersion></PropertyGroup>" +
                $"<Import Project=\"{imports}/VersionedGame.targets\"/></Project>");
            var args = new List<string> { "msbuild", project, "-t:ValidateVersionedGame", "-nologo", "-v:quiet" };
            if (requested.Length != 0) args.Add("-p:RimWorldVersion=" + requested);
            var result = new EndToEndProcessBuildCommandRunner().Run("dotnet", args, root, TimeSpan.FromSeconds(30));
            Assert.That(result.ExitCode == 0, Is.EqualTo(valid), result.StandardOutput + result.StandardError);
        }
        finally { Directory.Delete(root, true); }
    }

    [TestCase("1.6", "net480")]
    [TestCase("1.7", "net10.0")]
    public void DiscoverySelectsOnlyExplicitlyVersionedBundles(string version, string framework)
    {
        var root = Path.Combine(Path.GetTempPath(), "runtime-bundle-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            const string template = "<Project><PropertyGroup><TargetFramework>net480</TargetFramework>" +
                "<RimWorldEndToEndTest>true</RimWorldEndToEndTest>" +
                "<RimWorldEndToEndTestOwnerPackageId>test.bundle</RimWorldEndToEndTestOwnerPackageId>";
            File.WriteAllText(Path.Combine(root, "Versioned.csproj"), template +
                "<RimWorldVersionedBuild>true</RimWorldVersionedBuild></PropertyGroup></Project>");
            File.WriteAllText(Path.Combine(root, "Legacy.csproj"), template + "</PropertyGroup></Project>");
            var projects = EndToEndProjectDiscovery.Discover(root, version);
            Assert.That(projects.Single(p => p.AssemblyName == "Versioned").TargetFramework, Is.EqualTo(framework));
            Assert.That(projects.Single(p => p.AssemblyName == "Legacy").TargetFramework, Is.EqualTo("net480"));
            Assert.Throws<EndToEndDiscoveryException>(() => EndToEndProjectDiscovery.Discover(root, "1.8"));
        }
        finally { Directory.Delete(root, true); }
    }

    [Test]
    public void BetaBundleRequiresMatchingEvaluatedTarget()
    {
        var project = new EndToEndProjectRecord(
            Path.Combine(TestRepository.FindRoot(), "tests/RimWorldModding.Mcp.Tests/RimWorldModding.Mcp.Tests.csproj"),
            "test.bundle", "RimWorldModding.Mcp.Tests", "net10.0");
        // A command-boundary double supplies evaluated properties; metadata is read,
        // never executed, from this host-only test assembly.
        Assert.That(new EndToEndProjectBuilder(new PropertyRunner("net10.0"))
            .Build(new[] { project }, "Release", TimeSpan.FromSeconds(5)), Has.Count.EqualTo(1));
        Assert.Throws<EndToEndProjectBuildException>(() =>
            new EndToEndProjectBuilder(new PropertyRunner("net480"))
                .Build(new[] { project }, "Release", TimeSpan.FromSeconds(5)));
    }

    private sealed class PropertyRunner(string framework) : IEndToEndBuildCommandRunner
    {
        public List<IReadOnlyList<string>> Calls { get; } = new();
        public EndToEndBuildCommandResult Run(string fileName, IReadOnlyList<string> arguments,
            string workingDirectory, TimeSpan timeout)
        {
            Calls.Add(arguments);
            return new(0, arguments[0] == "build" ? "" :
                JsonSerializer.Serialize(new { Properties = new {
                    TargetPath = typeof(RuntimeBundleTests).Assembly.Location,
                    AssemblyName = "RimWorldModding.Mcp.Tests", TargetFramework = framework
                }}), "");
        }
    }

    [Test]
    public void SelectedInstallationReachesBothCompilerAndPropertyQuery()
    {
        var root = TestRepository.FindRoot();
        var runner = new PropertyRunner("net10.0");
        var project = new EndToEndProjectRecord(Path.Combine(root,
            "tests/RimWorldModding.Mcp.Tests/RimWorldModding.Mcp.Tests.csproj"),
            "test.bundle", "RimWorldModding.Mcp.Tests", "net10.0");
        new EndToEndProjectBuilder(runner, rimWorldPath: root).Build(new[] { project }, "Release", TimeSpan.FromSeconds(5));
        Assert.That(runner.Calls, Has.Count.EqualTo(2));
        foreach (var call in runner.Calls)
            Assert.That(call, Does.Contain("-p:RimWorldPath=" + root).And.Contain("-p:DefaultRimWorldPath=" + root));
    }
}
