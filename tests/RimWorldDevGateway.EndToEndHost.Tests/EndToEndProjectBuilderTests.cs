using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using RimWorldDevGateway.EndToEndHost;

namespace RimWorldDevGateway.EndToEndHost.Tests;

[TestFixture]
public sealed class EndToEndProjectBuilderTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string Configuration =
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    [Test]
    public void Builder_uses_argument_safe_commands_and_reads_the_evaluated_compiled_output()
    {
        var project = ProjectRecord();
        var runner = new RecordingCommandRunner(
            new EndToEndBuildCommandResult(0, "build ok", string.Empty),
            new EndToEndBuildCommandResult(0, PropertyOutput(AssemblyPath()), string.Empty));
        var builder = new EndToEndProjectBuilder(runner, "dotnet-test");

        var candidates = builder.Build(new[] { project }, Configuration, TimeSpan.FromSeconds(30));

        Assert.Multiple(() =>
        {
            Assert.That(candidates, Has.Count.EqualTo(1));
            Assert.That(candidates[0].Metadata.AssemblyName, Is.EqualTo("EndToEndHost.ValidFixtures"));
            Assert.That(runner.Invocations, Has.Count.EqualTo(2));
            Assert.That(runner.Invocations[0].FileName, Is.EqualTo("dotnet-test"));
            Assert.That(runner.Invocations[0].Arguments, Is.EqualTo(new[]
            {
                "build",
                project.ProjectPath,
                "--configuration",
                Configuration,
                "--nologo",
                "--verbosity",
                "minimal"
            }));
            Assert.That(runner.Invocations[1].Arguments, Does.Contain("-getProperty:TargetPath"));
            Assert.That(runner.Invocations.All(invocation => invocation.WorkingDirectory == Path.GetDirectoryName(project.ProjectPath)), Is.True);
        });
    }

    [Test]
    public void Builder_stops_on_a_failed_build_and_preserves_diagnostics()
    {
        var runner = new RecordingCommandRunner(
            new EndToEndBuildCommandResult(7, "partial output", "compiler failure"));

        var error = Assert.Throws<EndToEndProjectBuildException>(() =>
            new EndToEndProjectBuilder(runner).Build(
                new[] { ProjectRecord() },
                Configuration,
                TimeSpan.FromSeconds(30)));

        Assert.Multiple(() =>
        {
            Assert.That(error!.Message, Does.Contain("exit code 7").And.Contain("compiler failure"));
            Assert.That(runner.Invocations, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Builder_rejects_a_non_RimWorld_target_before_reading_metadata()
    {
        var runner = new RecordingCommandRunner(
            new EndToEndBuildCommandResult(0, "build ok", string.Empty),
            new EndToEndBuildCommandResult(
                0,
                PropertyOutput(AssemblyPath(), targetFramework: "net8.0"),
                string.Empty));

        var error = Assert.Throws<EndToEndProjectBuildException>(() =>
            new EndToEndProjectBuilder(runner).Build(
                new[] { ProjectRecord() },
                Configuration,
                TimeSpan.FromSeconds(30)));

        Assert.That(error!.Message, Does.Contain("net480").And.Contain("net8.0"));
    }

    [Test]
    public void Real_builder_compiles_and_discovers_the_fixture_without_executing_it()
    {
        var candidates = new EndToEndProjectBuilder().Build(
            new[] { ProjectRecord() },
            Configuration,
            TimeSpan.FromMinutes(2));

        Assert.That(candidates.Single().Metadata.Declarations.Select(test => test.Id), Is.EqualTo(new[]
        {
            "alpha.base-a",
            "alpha.optional",
            "alpha.base-b"
        }).AsCollection);
    }

    private static EndToEndProjectRecord ProjectRecord() => new(
        ProjectPath(),
        "alpha.mod",
        "EndToEndHost.ValidFixtures",
        "net480");

    private static string ProjectPath() => Path.Combine(
        RepositoryRoot,
        "tests",
        "Fixtures",
        "EndToEndHost.ValidFixtures",
        "EndToEndHost.ValidFixtures.csproj");

    private static string AssemblyPath() => Path.Combine(
        Path.GetDirectoryName(ProjectPath())!,
        "bin",
        Configuration,
        "net480",
        "EndToEndHost.ValidFixtures.dll");

    private static string PropertyOutput(string targetPath, string targetFramework = "net480") =>
        JsonSerializer.Serialize(new
        {
            Properties = new
            {
                TargetPath = targetPath,
                AssemblyName = "EndToEndHost.ValidFixtures",
                TargetFramework = targetFramework
            }
        });

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "RimWorldMods.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }

    private sealed class RecordingCommandRunner : IEndToEndBuildCommandRunner
    {
        private readonly Queue<EndToEndBuildCommandResult> results;

        public RecordingCommandRunner(params EndToEndBuildCommandResult[] results)
        {
            this.results = new Queue<EndToEndBuildCommandResult>(results);
        }

        public List<Invocation> Invocations { get; } = new();

        public EndToEndBuildCommandResult Run(
            string fileName,
            IReadOnlyList<string> arguments,
            string workingDirectory,
            TimeSpan timeout)
        {
            Invocations.Add(new Invocation(fileName, arguments.ToArray(), workingDirectory, timeout));
            return results.Dequeue();
        }
    }

    private sealed class Invocation
    {
        public Invocation(string fileName, IReadOnlyList<string> arguments, string workingDirectory, TimeSpan timeout)
        {
            FileName = fileName;
            Arguments = arguments;
            WorkingDirectory = workingDirectory;
            Timeout = timeout;
        }

        public string FileName { get; }

        public IReadOnlyList<string> Arguments { get; }

        public string WorkingDirectory { get; }

        public TimeSpan Timeout { get; }
    }
}
