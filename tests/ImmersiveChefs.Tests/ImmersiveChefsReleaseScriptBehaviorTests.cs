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

    [Test]
    public void Publisher_process_lease_is_null_while_the_launcher_has_not_created_any_artifact()
    {
        using var fixture = Fixture.Create();
        var gatewayRoot = Path.Combine(fixture.Root, "not-created-yet");
        var leasePath = Path.Combine(gatewayRoot, "publisher-process-lease.json");
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Read-Json", "Assert-RetainedProcessIdentity", "Get-ExactProcessStartUtcFromManifest", "Get-PublisherProcessLease" },
            "Set-StrictMode -Version Latest; $lease=Get-PublisherProcessLease -GatewayRoot " + Ps(gatewayRoot) + " -ProcessLeasePath " + Ps(leasePath) +
            "; if($null -ne $lease){throw 'unexpected lease'}; Write-Output 'waiting'");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("waiting"));
        });
    }

    [Test]
    public void Atomic_text_writer_replaces_an_existing_durable_publication_state()
    {
        using var fixture = Fixture.Create();
        var statePath = Path.Combine(fixture.Root, "publication-state.txt");
        var sentinelBackup = statePath + ".bak";
        File.WriteAllText(statePath, "submitted|plan|123", new UTF8Encoding(false));
        File.WriteAllText(sentinelBackup, "retained recovery evidence", new UTF8Encoding(false));
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Write-TextAtomically" },
            "Write-TextAtomically -Path " + Ps(statePath) + " -Value 'succeeded|plan|123'; Write-Output (Get-Content -LiteralPath " + Ps(statePath) + " -Raw)");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("succeeded|plan|123"));
            Assert.That(File.ReadAllText(sentinelBackup), Is.EqualTo("retained recovery evidence"));
            Assert.That(
                Directory.GetFiles(fixture.Root).Where(path =>
                    !string.Equals(path, statePath, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(path, sentinelBackup, StringComparison.OrdinalIgnoreCase) &&
                    Path.GetFileName(path).StartsWith("publication-state.txt.", StringComparison.OrdinalIgnoreCase)),
                Is.Empty);
        });
    }

    [Test]
    public void Newer_release_tool_revision_is_allowed_only_for_exact_post_submit_reconciliation()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Test-ReleaseSourceRevisionAllowed" },
            "$same=Test-ReleaseSourceRevisionAllowed 'candidate' 'candidate' $false 0 $false; " +
            "$recovery=Test-ReleaseSourceRevisionAllowed 'tool-fix' 'candidate' $true 3782589902 $true; " +
            "$differentPlan=Test-ReleaseSourceRevisionAllowed 'tool-fix' 'candidate' $false 3782589902 $true; " +
            "$idless=Test-ReleaseSourceRevisionAllowed 'tool-fix' 'candidate' $true 0 $true; " +
            "$divergent=Test-ReleaseSourceRevisionAllowed 'divergent' 'candidate' $true 3782589902 $false; " +
            "Write-Output ($same.ToString()+'|'+$recovery.ToString()+'|'+$differentPlan.ToString()+'|'+$idless.ToString()+'|'+$divergent.ToString())");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("True|True|False|False|False"));
        });
    }

    [Test]
    public void Reconciliation_requires_the_exact_nonzero_item_identity_from_durable_state()
    {
        using var fixture = Fixture.Create();
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsWorkshopRelease.ps1",
            new[] { "Test-ReconciliationStateIdentity" },
            "$exact=Test-ReconciliationStateIdentity 'submitted' 3782589902 3782589902; " +
            "$conflict=Test-ReconciliationStateIdentity 'submitted' 111 222; " +
            "$missing=Test-ReconciliationStateIdentity 'submitted' 0 3782589902; " +
            "$preSubmit=Test-ReconciliationStateIdentity 'create-admitted' 0 0; " +
            "Write-Output ($exact.ToString()+'|'+$conflict.ToString()+'|'+$missing.ToString()+'|'+$preSubmit.ToString())");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo("True|False|False|True"));
        });
    }

    [Test]
    public void Subscribed_smoke_selects_the_session_copy_that_owns_the_screenshots()
    {
        using var fixture = Fixture.Create();
        var runnerEvidence = Path.Combine(fixture.Root, "smoke-001", "run", "end-to-end-tests.json");
        var persistedEvidence = Path.Combine(fixture.Root, "smoke-001", "run", "SavedData", "DevGateway", "Sessions", "one", "end-to-end-tests.json");
        Directory.CreateDirectory(Path.GetDirectoryName(runnerEvidence)!);
        Directory.CreateDirectory(Path.GetDirectoryName(persistedEvidence)!);
        File.WriteAllText(runnerEvidence, "{}", new UTF8Encoding(false));
        File.WriteAllText(persistedEvidence, "{}", new UTF8Encoding(false));
        var run = fixture.InvokeFunctions(
            "Invoke-ImmersiveChefsSubscribedSmoke.ps1",
            new[] { "Get-SubscribedSmokeEvidenceFile" },
            "Write-Output ((Get-SubscribedSmokeEvidenceFile -RunRoot " + Ps(fixture.Root) + ").FullName)");
        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            Assert.That(run.StandardOutput.Trim(), Is.EqualTo(persistedEvidence));
        });
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
