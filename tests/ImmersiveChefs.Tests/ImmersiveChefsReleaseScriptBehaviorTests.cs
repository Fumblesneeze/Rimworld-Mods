using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
[NonParallelizable]
public sealed class ImmersiveChefsReleaseScriptBehaviorTests
{
    [Test]
    public void Staged_inventory_accepts_an_identity_already_in_the_manifest_without_double_counting_it()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Read-Json", "Get-RelativePath", "Assert-StagedCandidate" },
            $"Assert-StagedCandidate ([pscustomobject]@{{ packageManifestPath = {Ps(fixture.ManifestPath)}; packagePath = {Ps(fixture.PackageRoot)}; generatedAtPublication = @('About\\PublishedFileId.txt') }}); Write-Output 'accepted'");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("accepted"));
        });
    }

    [Test]
    public void Staged_inventory_rejects_an_unreviewed_extra_file()
    {
        using var fixture = Fixture.Create();
        File.WriteAllText(Path.Combine(fixture.PackageRoot, "unreviewed.txt"), "x", new UTF8Encoding(false));
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Read-Json", "Get-RelativePath", "Assert-StagedCandidate" },
            $"Assert-StagedCandidate ([pscustomobject]@{{ packageManifestPath = {Ps(fixture.ManifestPath)}; packagePath = {Ps(fixture.PackageRoot)}; generatedAtPublication = @() }})");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(1));
            Assert.That(run.StandardError, Does.Contain("Unexpected staged file"));
        });
    }

    [Test]
    public void Workshop_identity_is_recovered_from_one_receipt_and_conflicting_receipts_fail_closed()
    {
        using var fixture = Fixture.Create();
        var stateRoot = Path.Combine(fixture.Root, "state");
        var receiptRoot = Path.Combine(stateRoot, "publication", "one");
        Directory.CreateDirectory(receiptRoot);
        File.WriteAllText(Path.Combine(receiptRoot, "publication-receipt.json"),
            "{\"schema\":\"ImmersiveChefs/WorkshopPublicationReceipt/v1\",\"publishedFileId\":\"123456789\"}",
            new UTF8Encoding(false));
        var recovered = fixture.InvokeFunctions(
            "Build-ImmersiveChefsRelease.ps1",
            new[] { "Resolve-ExistingWorkshopIdentity" },
            $"Write-Output (Resolve-ExistingWorkshopIdentity -DeclaredId $null -StateRoot {Ps(stateRoot)})");
        Assert.Multiple(() =>
        {
            Assert.That(recovered.ExitCode, Is.Zero, recovered.StandardError);
            Assert.That(recovered.StandardOutput.Trim(), Is.EqualTo("123456789"));
        });

        var second = Path.Combine(stateRoot, "publication", "two");
        Directory.CreateDirectory(second);
        File.WriteAllText(Path.Combine(second, "publication-receipt.json"),
            "{\"schema\":\"ImmersiveChefs/WorkshopPublicationReceipt/v1\",\"publishedFileId\":\"987654321\"}",
            new UTF8Encoding(false));
        var conflict = fixture.InvokeFunctions(
            "Build-ImmersiveChefsRelease.ps1",
            new[] { "Resolve-ExistingWorkshopIdentity" },
            $"Write-Output (Resolve-ExistingWorkshopIdentity -DeclaredId $null -StateRoot {Ps(stateRoot)})");
        Assert.Multiple(() =>
        {
            Assert.That(conflict.ExitCode, Is.EqualTo(1));
            Assert.That(conflict.StandardError, Does.Contain("identity evidence conflicts"));
        });
    }

    [Test]
    public void Release_scripts_parse_under_the_installed_Windows_PowerShell_host()
    {
        using var fixture = Fixture.Create();
        foreach (var script in new[] { "Build-ImmersiveChefsRelease.ps1", "Invoke-ImmersiveChefsWorkshopRelease.ps1" })
        {
            var parsed = fixture.ParseWithWindowsPowerShell(script);
            Assert.That(parsed.ExitCode, Is.Zero, script + Environment.NewLine + parsed.StandardError);
        }
    }

    [Test]
    public void Publisher_process_lease_is_recovered_before_the_interactive_hold_exists()
    {
        using var fixture = Fixture.Create();
        var gatewayRoot = Path.Combine(fixture.Root, "gateway");
        var manifestDirectory = Path.Combine(gatewayRoot, "one", "SavedData", "DevGateway");
        Directory.CreateDirectory(manifestDirectory);
        using var retained = Process.Start(new ProcessStartInfo("notepad.exe") { UseShellExecute = false })
            ?? throw new InvalidOperationException("Could not start the retained-process fixture.");
        try
        {
            var started = retained.StartTime.ToUniversalTime().ToString("O");
            var leasePath = Path.Combine(gatewayRoot, "publisher-process-lease.json");
            File.WriteAllText(leasePath,
                "{\"schema\":\"RimWorldDevGateway/ProcessLease/v1\",\"processId\":" + retained.Id + ",\"processStartUtc\":\"" + started + "\"}",
                new UTF8Encoding(false));

            var run = fixture.InvokeFunctions(
                "Invoke-ImmersiveChefsWorkshopRelease.ps1",
                new[] { "Read-Json", "Assert-RetainedProcessIdentity", "Get-ExactProcessStartUtcFromManifest", "Get-PublisherProcessLease" },
                "$lease=Get-PublisherProcessLease -GatewayRoot " + Ps(gatewayRoot) + " -ProcessLeasePath " + Ps(leasePath) +
                "; Write-Output ($lease.ProcessId.ToString() + '|' + [IO.Path]::GetFileName($lease.ManifestPath))");
            Assert.Multiple(() =>
            {
                Assert.That(run.ExitCode, Is.Zero, run.StandardError);
                Assert.That(run.StandardOutput.Trim(), Is.EqualTo(retained.Id + "|publisher-process-lease.json"));
            });
        }
        finally
        {
            if (!retained.HasExited)
            {
                retained.Kill();
                retained.WaitForExit();
            }
        }
    }

    [Test]
    public void Publisher_process_lease_rejects_even_a_subsecond_start_identity_mismatch()
    {
        using var fixture = Fixture.Create();
        using var retained = Process.Start(new ProcessStartInfo("notepad.exe") { UseShellExecute = false })
            ?? throw new InvalidOperationException("Could not start the retained-process fixture.");
        try
        {
            var expected = retained.StartTime.ToUniversalTime().AddMilliseconds(1).ToString("O");
            var run = fixture.InvokeFunctions(
                "Invoke-ImmersiveChefsWorkshopRelease.ps1",
                new[] { "Assert-RetainedProcessIdentity" },
                "Assert-RetainedProcessIdentity -Process (Get-Process -Id " + retained.Id + ") -ExpectedStartUtc ([datetimeoffset]::ParseExact(" +
                Ps(expected) + ",'O',[Globalization.CultureInfo]::InvariantCulture))");
            Assert.Multiple(() =>
            {
                Assert.That(run.ExitCode, Is.EqualTo(1));
                Assert.That(run.StandardError, Does.Contain("PID was reused"));
            });
        }
        finally
        {
            if (!retained.HasExited)
            {
                retained.Kill();
                retained.WaitForExit();
            }
        }
    }

    private static string Ps(string value) => "'" + value.Replace("'", "''") + "'";

    private sealed class Fixture : IDisposable
    {
        private Fixture(string root, string repositoryRoot)
        {
            Root = root;
            RepositoryRoot = repositoryRoot;
            PackageRoot = Path.Combine(root, "package");
            Directory.CreateDirectory(Path.Combine(PackageRoot, "About"));
            var identity = Path.Combine(PackageRoot, "About", "PublishedFileId.txt");
            File.WriteAllText(identity, "123456789", new UTF8Encoding(false));
            string hash;
            using (var sha = System.Security.Cryptography.SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(identity))).Replace("-", string.Empty);
            ManifestPath = Path.Combine(root, "package-files.json");
            File.WriteAllText(ManifestPath,
                "{\"files\":[{\"path\":\"About\\\\PublishedFileId.txt\",\"bytes\":9,\"sha256\":\"" + hash + "\"}]}",
                new UTF8Encoding(false));
        }

        public string Root { get; }
        public string RepositoryRoot { get; }
        public string PackageRoot { get; }
        public string ManifestPath { get; }

        public static Fixture Create()
        {
            var root = Path.Combine(Path.GetTempPath(), nameof(ImmersiveChefsReleaseScriptBehaviorTests), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new Fixture(root, FindRepositoryRoot());
        }

        public Invocation InvokeFunctions(string scriptName, IReadOnlyList<string> functionNames, string operation)
        {
            var target = Path.Combine(RepositoryRoot, "scripts", scriptName);
            var invocation = Path.Combine(Root, "invoke-" + Guid.NewGuid().ToString("N") + ".ps1");
            var script =
                "$ErrorActionPreference='Stop'\n$tokens=$null;$errors=$null\n" +
                $"$ast=[System.Management.Automation.Language.Parser]::ParseFile({Ps(target)},[ref]$tokens,[ref]$errors)\n" +
                $"foreach($name in @({string.Join(",", functionNames.Select(Ps))})){{ $node=$ast.Find({{param($n) $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq $name}},$true); if($null -eq $node){{throw \"missing $name\"}}; Invoke-Expression $node.Extent.Text }}\n" +
                "try {\n" + operation + "\nexit 0\n} catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }\n";
            File.WriteAllText(invocation, script, new UTF8Encoding(false));
            return Run("pwsh.exe", $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{invocation}\"");
        }

        public Invocation ParseWithWindowsPowerShell(string scriptName)
        {
            var target = Path.Combine(RepositoryRoot, "scripts", scriptName);
            var invocation = Path.Combine(Root, "parse-" + Guid.NewGuid().ToString("N") + ".ps1");
            var script =
                "$errors=$null;$tokens=$null\n" +
                $"$null=[System.Management.Automation.Language.Parser]::ParseFile({Ps(target)},[ref]$tokens,[ref]$errors)\n" +
                "if($errors.Count -ne 0){$errors | ForEach-Object {[Console]::Error.WriteLine($_.Message)};exit 1}\nexit 0\n";
            File.WriteAllText(invocation, script, new UTF8Encoding(false));
            return Run("powershell.exe", $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{invocation}\"");
        }

        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }

        private static Invocation Run(string file, string arguments)
        {
            var start = new ProcessStartInfo(file, arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start PowerShell.");
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30_000)) { process.Kill(); throw new TimeoutException("PowerShell fixture timed out."); }
            Task.WaitAll(stdout, stderr);
            return new Invocation(process.ExitCode, stdout.Result, stderr.Result);
        }

        private static string FindRepositoryRoot()
        {
            for (var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); current is not null; current = current.Parent)
                if (File.Exists(Path.Combine(current.FullName, "ImmersiveChefs.sln"))) return current.FullName;
            throw new DirectoryNotFoundException("Could not find repository root.");
        }
    }

    private sealed class Invocation
    {
        public Invocation(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }
        public int ExitCode { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }
    }
}
