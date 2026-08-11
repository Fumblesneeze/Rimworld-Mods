using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
[NonParallelizable]
public sealed class RunnerContractTests
{
    [Test]
    public void Grouped_run_continues_after_an_early_failure_and_aggregates_every_suite()
    {
        using var run = FakeDotNetRun.Start("early-failure", "ImmersiveChefs");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.Json["Status"], Is.EqualTo("failed"));
            Assert.That(run.InvokedSuites, Is.EqualTo(new[]
            {
                "ImmersiveChefs.Unit",
                "ImmersiveChefs.Harmony",
                "ImmersiveChefs.Defs"
            }));
            Assert.That(run.Suites.Select(SuiteStatus), Is.EqualTo(new[]
            {
                "ImmersiveChefs.Unit:failed",
                "ImmersiveChefs.Harmony:passed",
                "ImmersiveChefs.Defs:passed"
            }));
        });
    }

    [Test]
    public void Zero_executed_result_fails_even_when_the_test_process_succeeds()
    {
        using var run = FakeDotNetRun.Start("zero-executed", "ImmersiveChefs.Unit");
        var suite = run.Suites.Single();

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.Json["Status"], Is.EqualTo("failed"));
            Assert.That(Convert.ToInt32(suite["Total"]), Is.EqualTo(1));
            Assert.That(Convert.ToInt32(suite["Executed"]), Is.Zero);
            Assert.That(Convert.ToInt32(suite["NotExecuted"]), Is.EqualTo(1));
        });
    }

    [Test]
    public void Passing_group_returns_parseable_json_and_success()
    {
        using var run = FakeDotNetRun.Start("all-pass", "ImmersiveChefs");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero);
            Assert.That(run.Json["Status"], Is.EqualTo("passed"));
            Assert.That(run.Suites, Has.Count.EqualTo(3));
            Assert.That(run.Suites.Select(SuiteStatus), Has.All.EndsWith(":passed"));
        });
    }

    [Test]
    public void Gateway_group_includes_the_exact_installed_Circinus_shape_suite()
    {
        using var run = FakeDotNetRun.Start("all-pass", "RimWorldDevGateway");

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero);
            Assert.That(run.Json["Status"], Is.EqualTo("passed"));
            Assert.That(run.InvokedSuites, Is.EqualTo(new[]
            {
                "RimWorldDevGateway.Unit",
                "RimWorldDevGateway.Snapshots",
                "RimWorldDevGateway.CircinusShape"
            }));
        });
    }

    private static string SuiteStatus(IDictionary<string, object> suite)
    {
        return $"{suite["Suite"]}:{suite["Status"]}";
    }

    private sealed class FakeDotNetRun : IDisposable
    {
        private readonly string temporaryDirectory;

        private FakeDotNetRun(
            string temporaryDirectory,
            int exitCode,
            IDictionary<string, object> json,
            IReadOnlyList<IDictionary<string, object>> suites,
            IReadOnlyList<string> invokedSuites)
        {
            this.temporaryDirectory = temporaryDirectory;
            ExitCode = exitCode;
            Json = json;
            Suites = suites;
            InvokedSuites = invokedSuites;
        }

        public int ExitCode { get; }

        public IDictionary<string, object> Json { get; }

        public IReadOnlyList<IDictionary<string, object>> Suites { get; }

        public IReadOnlyList<string> InvokedSuites { get; }

        public static FakeDotNetRun Start(string mode, string suiteSelection)
        {
            var repositoryRoot = FindRepositoryRoot();
            var runnerPath = Path.Combine(repositoryRoot, "scripts", "Invoke-Tests.ps1");
            var fakeDotNetDirectory = Path.Combine(
                repositoryRoot,
                "tests",
                "ImmersiveChefs.Tests",
                "Fixtures",
                "FakeDotnet");
            var rimWorldPath = GetAssemblyMetadata("RimWorldPath");
            var steamModContentFolder = GetAssemblyMetadata("SteamModContentFolder");
            var temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                "ImmersiveChefsRunnerContract",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);
            var journalPath = Path.Combine(temporaryDirectory, "invocations.txt");

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "pwsh.exe",
                    Arguments =
                        $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File {Quote(runnerPath)} " +
                        $"-Suite {Quote(suiteSelection)} -Configuration Release -Output json " +
                        $"-RimWorldPath {Quote(rimWorldPath)} " +
                        $"-SteamModContentFolder {Quote(steamModContentFolder)}",
                    WorkingDirectory = repositoryRoot,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                startInfo.EnvironmentVariables["PATH"] =
                    fakeDotNetDirectory + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
                startInfo.EnvironmentVariables["IC_TEST_FAKE_DOTNET_MODE"] = mode;
                startInfo.EnvironmentVariables["IC_TEST_FAKE_DOTNET_JOURNAL"] = journalPath;

                using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the runner contract process.");
                var standardOutput = process.StandardOutput.ReadToEndAsync();
                var standardError = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(60000))
                {
                    process.Kill();
                    throw new TimeoutException("The runner contract process did not exit within 60 seconds.");
                }

                Task.WaitAll(standardOutput, standardError);
                var output = standardOutput.Result;
                var error = standardError.Result;
                var jsonLine = output
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .SingleOrDefault(line => line.TrimStart().StartsWith("{", StringComparison.Ordinal));
                if (jsonLine is null)
                {
                    throw new AssertionException($"Runner emitted no JSON. stdout: {output} stderr: {error}");
                }

                var serializer = new JavaScriptSerializer();
                var json = (IDictionary<string, object>)serializer.DeserializeObject(jsonLine);
                var suites = ((IEnumerable)json["Suites"])
                    .Cast<IDictionary<string, object>>()
                    .ToArray();
                var invocations = File.Exists(journalPath)
                    ? File.ReadAllLines(journalPath).Where(line => line.Length > 0).ToArray()
                    : Array.Empty<string>();

                return new FakeDotNetRun(
                    temporaryDirectory,
                    process.ExitCode,
                    json,
                    suites,
                    invocations);
            }
            catch
            {
                try
                {
                    Directory.Delete(temporaryDirectory, recursive: true);
                }
                catch
                {
                    // Preserve the runner-contract failure; this directory contains no user data.
                }

                throw;
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
        }

        private static string FindRepositoryRoot()
        {
            for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
                 directory is not null;
                 directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "scripts", "Invoke-Tests.ps1")))
                {
                    return directory.FullName;
                }
            }

            throw new DirectoryNotFoundException("Could not find the repository root from the NUnit test directory.");
        }

        private static string Quote(string value)
        {
            return $"\"{value.Replace("\"", "\\\"")}\"";
        }

        private static string GetAssemblyMetadata(string key)
        {
            return typeof(RunnerContractTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(attribute => attribute.Key == key)
                .Value;
        }
    }
}
