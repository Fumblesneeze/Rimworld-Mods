using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace RimWorldDevGateway.EndToEndHost.Tests;

[TestFixture]
public sealed class PerformanceProjectHostTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string Configuration =
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    [Test]
    public void Discovery_selects_only_literal_marked_performance_projects()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "performance-projects", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "marked"));
        Directory.CreateDirectory(Path.Combine(root, "ordinary"));
        Directory.CreateDirectory(Path.Combine(root, "ignored", "obj"));
        File.WriteAllText(Path.Combine(root, "marked", "Marked.csproj"), ProjectXml(marked: true));
        File.WriteAllText(Path.Combine(root, "ordinary", "Ordinary.csproj"), ProjectXml(marked: false));
        File.WriteAllText(Path.Combine(root, "ignored", "obj", "Ignored.csproj"), ProjectXml(marked: true));
        try
        {
            var projects = PerformanceProjectDiscovery.Discover(root);

            Assert.Multiple(() =>
            {
                Assert.That(projects, Has.Count.EqualTo(1));
                Assert.That(projects[0].OwnerPackageId, Is.EqualTo("alpha.mod"));
                Assert.That(projects[0].AssemblyName, Is.EqualTo("Performance.Marked"));
                Assert.That(projects[0].TargetFramework, Is.EqualTo("net480"));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Builder_uses_argument_safe_commands_and_reads_performance_metadata()
    {
        var record = Record();
        var runner = new RecordingCommandRunner(
            new EndToEndBuildCommandResult(0, "build ok", string.Empty),
            new EndToEndBuildCommandResult(0, PropertyOutput(), string.Empty));

        var candidates = new EndToEndProjectBuilder(runner, "dotnet-test").BuildPerformance(
            new[] { record }, Configuration, TimeSpan.FromSeconds(30));

        Assert.Multiple(() =>
        {
            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(candidates[0].Metadata.Declarations, Is.Not.Empty);
            Assert.That(candidates[0].ExpectedOwnerPackageId, Is.EqualTo("alpha.mod"));
            Assert.That(runner.Invocations, Has.Count.EqualTo(2));
            Assert.That(runner.Invocations[0].Arguments.Take(2), Is.EqualTo(new[] { "build", record.ProjectPath }));
        });
    }

    [Test]
    public void Builder_rejects_an_over_limit_project_sequence_before_starting_a_build()
    {
        var runner = new RecordingCommandRunner();
        var projects = Enumerable.Repeat(
            Record(),
            PerformanceDiscoveryValidator.MaximumBenchmarks + 1);

        var exception = Assert.Throws<EndToEndProjectBuildException>(() =>
            new EndToEndProjectBuilder(runner).BuildPerformance(
                projects,
                Configuration,
                TimeSpan.FromSeconds(30)));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message,
                Does.Contain(PerformanceDiscoveryValidator.MaximumBenchmarks.ToString()).And.Contain("before any build"));
            Assert.That(runner.Invocations, Is.Empty);
        });
    }

    [Test]
    public void Exact_stage_project_selection_excludes_unselected_projects_before_any_build()
    {
        var selected = Record();
        var broken = new PerformanceProjectRecord(
            Path.Combine(Path.GetTempPath(), "unselected-broken.csproj"),
            "optional.broken", "Optional.Broken.PerformanceTests", "net480");
        var projects = PerformanceProjectDiscovery.SelectExactProjects(
            new[] { broken, selected },
            new[] { selected.ProjectPath });
        var runner = new RecordingCommandRunner(
            new EndToEndBuildCommandResult(0, "build ok", string.Empty),
            new EndToEndBuildCommandResult(0, PropertyOutput(), string.Empty));

        var candidates = new EndToEndProjectBuilder(runner, "dotnet-test").BuildPerformance(
            projects, Configuration, TimeSpan.FromSeconds(30));

        Assert.Multiple(() =>
        {
            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(candidates[0].ProjectPath, Is.EqualTo(selected.ProjectPath));
            Assert.That(runner.Invocations, Has.Count.EqualTo(2));
            Assert.That(runner.Invocations.SelectMany(item => item.Arguments),
                Has.None.EqualTo(broken.ProjectPath));
        });
    }

    private static PerformanceProjectRecord Record() => new(
        ProjectPath(), "alpha.mod", "PerformanceHost.ValidFixtures", "net480");

    private static string ProjectPath() => Path.Combine(
        RepositoryRoot, "tests", "Fixtures", "PerformanceHost.ValidFixtures", "PerformanceHost.ValidFixtures.csproj");

    private static string AssemblyPath() => Path.Combine(
        Path.GetDirectoryName(ProjectPath())!, "bin", Configuration, "net480", "PerformanceHost.ValidFixtures.dll");

    private static string PropertyOutput() => JsonSerializer.Serialize(new
    {
        Properties = new
        {
            TargetPath = AssemblyPath(),
            AssemblyName = "PerformanceHost.ValidFixtures",
            TargetFramework = "net480"
        }
    });

    private static string ProjectXml(bool marked) =>
        "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>" +
        "<TargetFramework>net480</TargetFramework>" +
        "<AssemblyName>Performance.Marked</AssemblyName>" +
        (marked
            ? "<RimWorldPerformanceTest>true</RimWorldPerformanceTest>" +
              "<RimWorldPerformanceTestOwnerPackageId>alpha.mod</RimWorldPerformanceTestOwnerPackageId>"
            : string.Empty) +
        "</PropertyGroup></Project>";

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "RimWorldMods.sln"))) return directory.FullName;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }

    private sealed class RecordingCommandRunner : IEndToEndBuildCommandRunner
    {
        private readonly Queue<EndToEndBuildCommandResult> results;
        public RecordingCommandRunner(params EndToEndBuildCommandResult[] results) =>
            this.results = new Queue<EndToEndBuildCommandResult>(results);
        public List<Invocation> Invocations { get; } = new();
        public EndToEndBuildCommandResult Run(
            string fileName,
            IReadOnlyList<string> arguments,
            string workingDirectory,
            TimeSpan timeout)
        {
            Invocations.Add(new Invocation(arguments.ToArray()));
            return results.Dequeue();
        }
    }

    private sealed record Invocation(IReadOnlyList<string> Arguments);
}
