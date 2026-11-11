using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
[NonParallelizable]
public sealed class RimWorldEndToEndRunnerCliTests
{
    [TestCase("1.6", true)]
    [TestCase("1.7", false)]
    public void Native_legacy_flag_is_refused_outside_the_supported_E2E_launch(string version, bool e2e)
    {
        var root = Path.Combine(Path.GetTempPath(), "NativeXmlLoaderRefusal", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var smoke = Path.Combine(Path.GetDirectoryName(RunnerPath())!, "Invoke-GatewaySmoke.ps1");
            var source = string.Join(Environment.NewLine, new[]
            {
                "$ErrorActionPreference='Stop'",
                "$tokens=$null; $errors=$null; $ast=[Management.Automation.Language.Parser]::ParseFile(" + PowerShellLiteral(smoke) + ",[ref]$tokens,[ref]$errors)",
                "$function=$ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Get-GatewayNativeXmlLoaderArguments'},$true); . ([scriptblock]::Create($function.Extent.Text))",
                "Get-GatewayNativeXmlLoaderArguments -LegacyXmlDeserializer:$true -RunEndToEndTests:$" + (e2e ? "true" : "false") + " -RimWorldMajorVersion " + PowerShellLiteral(version)
            });
            var result = InvokeSource(source, root);
            Assert.That(result.ExitCode, Is.Not.Zero);
            Assert.That(result.StandardError, Does.Contain("isolated RimWorld 1.7 E2E run"));
            Assert.That(result.StandardOutput, Is.Empty);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Native_launch_argument_array_consumes_the_validated_loader_choice(bool legacy)
    {
        var root = Path.Combine(Path.GetTempPath(), "NativeXmlLoaderLaunch", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var smoke = Path.Combine(Path.GetDirectoryName(RunnerPath())!, "Invoke-GatewaySmoke.ps1");
            var source = string.Join(Environment.NewLine, new[]
            {
                "$ErrorActionPreference='Stop'",
                "$tokens=$null; $errors=$null; $ast=[Management.Automation.Language.Parser]::ParseFile(" + PowerShellLiteral(smoke) + ",[ref]$tokens,[ref]$errors)",
                "$function=$ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Get-GatewayNativeXmlLoaderArguments'},$true); . ([scriptblock]::Create($function.Extent.Text))",
                "$nativeXmlLoaderArguments=@(Get-GatewayNativeXmlLoaderArguments -LegacyXmlDeserializer:$" + (legacy ? "true" : "false") + " -RunEndToEndTests:$true -RimWorldMajorVersion '1.7')",
                "$savedDataPath='owned-save';$playerLogPath='owned-log';$unityWindowArguments=@('-screen-fullscreen','0');$rimWorldMajorVersion='1.7';$Quicktest=$true;$RunIntegrationTests=$false;$RunEndToEndTests=$true;$selectedEndToEndTestIds=@('exact.test');$IntegrationFailureProbe=$false",
                "$text=$ast.Extent.Text;$begin=$text.IndexOf('$launchArguments = @(');$end=$text.IndexOf('try {',$begin); . ([scriptblock]::Create($text.Substring($begin,$end-$begin))); @{arguments=$launchArguments}|ConvertTo-Json -Compress"
            });
            var result = InvokeSource(source, root);
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            using var json = JsonDocument.Parse(result.StandardOutput);
            var arguments = json.RootElement.GetProperty("arguments").EnumerateArray().Select(value => value.GetString()).ToArray();
            Assert.That(arguments.Count(value => value == "-legacy-xml-deserializer"), Is.EqualTo(legacy ? 1 : 0));
            Assert.That(arguments, Does.Contain("-quicktest").And.Contain("-devGatewayEndToEndTestIds=exact.test"));
            Assert.That(arguments.First(), Does.Contain("owned-save"));
        }
        finally { Directory.Delete(root, recursive: true); }
    }
    [TestCase(false)]
    [TestCase(true)]
    public void Native_xml_loader_argument_projection_preserves_default_and_emits_one_fixed_flag(bool legacy)
    {
        var root = Path.Combine(Path.GetTempPath(), "NativeXmlLoaderArguments", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var smoke = Path.Combine(Path.GetDirectoryName(RunnerPath())!, "Invoke-GatewaySmoke.ps1");
            var source = string.Join(Environment.NewLine, new[]
            {
                "$ErrorActionPreference='Stop'",
                "$tokens=$null; $errors=$null; $ast=[Management.Automation.Language.Parser]::ParseFile(" + PowerShellLiteral(smoke) + ",[ref]$tokens,[ref]$errors)",
                "$function=$ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Get-GatewayNativeXmlLoaderArguments'},$true); . ([scriptblock]::Create($function.Extent.Text))",
                "$arguments=@(Get-GatewayNativeXmlLoaderArguments -LegacyXmlDeserializer:$" + (legacy ? "true" : "false") + " -RunEndToEndTests:$true -RimWorldMajorVersion '1.7'); @{arguments=$arguments} | ConvertTo-Json -Compress"
            });
            var result = InvokeSource(source, root);
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            using var json = JsonDocument.Parse(result.StandardOutput);
            Assert.That(json.RootElement.GetProperty("arguments").EnumerateArray().Select(value => value.GetString()),
                Is.EqualTo(legacy ? new[] { "-legacy-xml-deserializer" } : Array.Empty<string>()));
        }
        finally { Directory.Delete(root, recursive: true); }
    }
    [TestCase(false, false, false)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, true, false)]
    [TestCase(false, false, true)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(true, true, true)]
    public void Dry_run_projects_selected_plan_mode_and_fixture_inventory(bool includesFixture, bool legacy, bool selectByGroup)
    {
        var root = Path.Combine(Path.GetTempPath(), "RimWorldEndToEndRunnerCliTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var selectedId = legacy ? "alpha.legacy" : "alpha.default";
            var selectedGroup = "ludeon.rimworld|alpha.mod" + (legacy ? "::legacy-xml" : "");
            var source = string.Join(Environment.NewLine, new[]
            {
                "$ErrorActionPreference='Stop'",
                "Import-Module " + PowerShellLiteral(SupportModulePath()) + " -Force",
                "$tokens=$null; $errors=$null; $ast=[Management.Automation.Language.Parser]::ParseFile(" + PowerShellLiteral(RunnerPath()) + ",[ref]$tokens,[ref]$errors)",
                "foreach($name in @('Write-RunnerResult','Get-SafeGroupDirectoryName','ConvertFrom-RequiredJson','Exit-InvalidInput')) { $function=$ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name},$true); . ([scriptblock]::Create($function.Extent.Text)) }",
                "$artifactRoot=" + PowerShellLiteral(root),
                "$fixture=@{ownerPackageId='alpha.mod';packageId='alpha.fixture';rimWorldVersion='1.7';manifestPath='tests/Fixtures/Alpha/fixture.json';files=@(@{relativePath='About/About.xml';length=123;sha256='ABC123'},@{relativePath='Defs/Diagnostic.xml';length=456;sha256='DEF456'})}",
                "$groups=@(@{groupId='ludeon.rimworld|alpha.mod';xmlDeserializer='default';activePackageIds=@('ludeon.rimworld','alpha.mod');tests=@('alpha.default');xmlFixtures=@(" + (includesFixture ? "$fixture" : "") + ")},@{groupId='ludeon.rimworld|alpha.mod::legacy-xml';xmlDeserializer='legacy';activePackageIds=@('ludeon.rimworld','alpha.mod');tests=@('alpha.legacy');xmlFixtures=@(" + (includesFixture ? "$fixture" : "") + ")})",
                "$planInvocation=@{StandardOutput=(@{owners=@();groups=$groups}|ConvertTo-Json -Depth 8 -Compress)}",
                "$GroupId=@(" + (selectByGroup ? PowerShellLiteral(selectedGroup) : "") + "); $testIdFilters=@(" + (selectByGroup ? "" : PowerShellLiteral(selectedId)) + ")",
                "$Output='json'; $DryRun=$true; $packageIds=@('ludeon.rimworld','alpha.mod'); $EnableAudio=$false; $Language='English'; $TimeoutSeconds=300; $PrelaunchConfigDirectory=''; $smokeScript='smoke.ps1'; $resolvedRimWorldPath='game'; $resolvedWorkshopPath='workshop'",
                "$text=[IO.File]::ReadAllText(" + PowerShellLiteral(RunnerPath()) + "); $begin=$text.IndexOf('$plan = ConvertFrom-RequiredJson'); $end=$text.IndexOf('$existingRimWorld =', $begin); & ([scriptblock]::Create($text.Substring($begin,$end-$begin)))"
            });
            var run = InvokeSource(source, root);
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            using var json = JsonDocument.Parse(run.StandardOutput);
            var result = json.RootElement;
            Assert.That(result.GetProperty("MutatedGame").GetBoolean(), Is.False);
            Assert.That(result.GetProperty("AvailablePackageCount").GetInt32(), Is.EqualTo(2));
            Assert.That(result.GetProperty("Groups").GetArrayLength(), Is.EqualTo(1));
            var group = result.GetProperty("Groups")[0];
            Assert.That(group.GetProperty("GroupId").GetString(), Is.EqualTo(selectedGroup));
            Assert.That(group.GetProperty("Tests").EnumerateArray().Select(value => value.GetString()), Is.EqualTo(new[] { selectedId }));
            Assert.That(group.GetProperty("XmlDeserializer").GetString(), Is.EqualTo(legacy ? "legacy" : "default"));
            var command = group.GetProperty("Command").EnumerateArray().Select(value => value.GetString()).ToArray();
            Assert.That(command.Count(value => value == "-LegacyXmlDeserializer"), Is.EqualTo(legacy ? 1 : 0));
            Assert.That(command, Does.Contain("-EndToEndTestIds").And.Contain(selectedId));
            var fixtures = result.GetProperty("Groups")[0].GetProperty("XmlFixtures");
            Assert.That(fixtures.GetArrayLength(), Is.EqualTo(includesFixture ? 1 : 0));
            if (includesFixture)
            {
                Assert.That(fixtures[0].GetProperty("packageId").GetString(), Is.EqualTo("alpha.fixture"));
                Assert.That(fixtures[0].GetProperty("ownerPackageId").GetString(), Is.EqualTo("alpha.mod"));
                Assert.That(fixtures[0].GetProperty("manifestPath").GetString(), Is.EqualTo("tests/Fixtures/Alpha/fixture.json"));
                var files = fixtures[0].GetProperty("files");
                Assert.That(files.GetArrayLength(), Is.EqualTo(2));
                Assert.That(files[0].GetProperty("relativePath").GetString(), Is.EqualTo("About/About.xml"));
                Assert.That(files[0].GetProperty("length").GetInt64(), Is.EqualTo(123));
                Assert.That(files[0].GetProperty("sha256").GetString(), Is.EqualTo("ABC123"));
                Assert.That(files[1].GetProperty("relativePath").GetString(), Is.EqualTo("Defs/Diagnostic.xml"));
                Assert.That(files[1].GetProperty("length").GetInt64(), Is.EqualTo(456));
                Assert.That(files[1].GetProperty("sha256").GetString(), Is.EqualTo("DEF456"));
            }
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Test]
    public void Failed_launch_result_and_cleanup_failure_both_reach_group_evidence()
    {
        var root = Path.Combine(Path.GetTempPath(), "RimWorldEndToEndRunnerCliTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var source = string.Join(Environment.NewLine, new[]
            {
                "$ErrorActionPreference='Stop'",
                "Import-Module " + PowerShellLiteral(SupportModulePath()) + " -Force",
                "$tokens=$null; $errors=$null; $ast=[Management.Automation.Language.Parser]::ParseFile(" + PowerShellLiteral(RunnerPath()) + ",[ref]$tokens,[ref]$errors)",
                "$function=$ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Invoke-BoundedProcess'},$true); . ([scriptblock]::Create($function.Extent.Text))",
                "$catchBody=$ast.Find({param($node) $node -is [Management.Automation.Language.CatchClauseAst] -and $node.Body.Extent.Text.Contains('$groupFailure =')},$true).Body.Extent.Text; $catchBody=$catchBody.Substring(1,$catchBody.Length-2)",
                "$root=" + PowerShellLiteral(root),
                "$lease=Join-Path $root 'group-001/fixture.lease.json'; $identity=Join-Path $root 'smoke-001/game-process.json'; $null=New-Item (Split-Path $lease),(Split-Path $identity) -ItemType Directory -Force",
                "$groupResults=[Collections.Generic.List[object]]::new(); $group=@{groupId='fixture';activePackageIds=@('ludeon.rimworld');tests=@('fixture.test')}; $Language='English'; $smokeLaunch=@{AudioEnabled=$false}; $stdoutPath=Join-Path $root 'stdout.json'; $stderrPath=Join-Path $root 'stderr.txt'",
                "try { $null=Invoke-RimWorldEndToEndFixtureLaunch -RunDirectory $root -FixtureLeaseFile $lease -ProcessLeaseFile $identity -Stage { Set-Content $lease 'owned fixture' } -Launch { Invoke-BoundedProcess -ExecutablePath pwsh -Arguments @('-NoProfile','-Command','Start-Sleep -Milliseconds 200; [Console]::Out.WriteLine(''{\"Status\":\"failed\",\"Detail\":\"native failure\"}''); [Console]::Error.WriteLine(''native-stderr''); exit 17') -TimeoutMilliseconds 10000 -Description child -ProcessLeaseFile $identity } -Clean { throw 'cleanup locked' } } catch { & ([scriptblock]::Create($catchBody)) }",
                "@{record=$groupResults[0];stdout=[IO.File]::ReadAllText($stdoutPath);stderr=[IO.File]::ReadAllText($stderrPath)} | ConvertTo-Json -Depth 8 -Compress"
            });
            var run = InvokeSource(source, root);
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            using var json = JsonDocument.Parse(run.StandardOutput);
            var record = json.RootElement.GetProperty("record");
            Assert.That(record.GetProperty("ExitCode").GetInt32(), Is.EqualTo(17));
            Assert.That(record.GetProperty("Message").GetString(), Does.Contain("cleanup locked"));
            Assert.That(record.GetProperty("SmokeResult").GetProperty("Detail").GetString(), Is.EqualTo("native failure"));
            Assert.That(json.RootElement.GetProperty("stdout").GetString(), Does.Contain("native failure"));
            Assert.That(json.RootElement.GetProperty("stderr").GetString(), Does.Contain("native-stderr").And.Contain("cleanup locked"));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Test]
    public void External_fixture_publisher_is_leased_and_confirmed_before_game_invocation()
    {
        var root = Path.Combine(Path.GetTempPath(), "RimWorldEndToEndRunnerCliTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var source = string.Join(Environment.NewLine, new[]
            {
                "$ErrorActionPreference='Stop'",
                "Import-Module " + PowerShellLiteral(SupportModulePath()) + " -Force",
                "$tokens=$null; $errors=$null; $ast=[Management.Automation.Language.Parser]::ParseFile(" + PowerShellLiteral(RunnerPath()) + ",[ref]$tokens,[ref]$errors)",
                "$function=$ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Invoke-BoundedProcess'},$true); . ([scriptblock]::Create($function.Extent.Text))",
                "$root=" + PowerShellLiteral(root),
                "$lease=Join-Path $root 'group-001/fixture.lease.json'; $identity=Join-Path $root 'smoke-001/game-process.json'; $stageIdentity=Join-Path $root 'group-001/fixture-stage-process.json'",
                "$null=New-Item (Split-Path $lease),(Split-Path $identity) -ItemType Directory -Force",
                "$result=Invoke-RimWorldEndToEndFixtureLaunch -ExternalStage -RunDirectory $root -FixtureLeaseFile $lease -ProcessLeaseFile $identity -Stage {",
                " $catalog=Get-Content (Join-Path $root 'fixture-recovery.json') -Raw | ConvertFrom-Json; if(-not $catalog.groups[0].stageLaunchAttempted -or $catalog.groups[0].launchAttempted){throw 'bad publication intent'}",
                " $null=Invoke-BoundedProcess -ExecutablePath pwsh -Arguments @('-NoProfile','-Command','Start-Sleep -Milliseconds 200') -TimeoutMilliseconds 10000 -Description publisher -ProcessLeaseFile $stageIdentity; Set-Content $lease 'owned fixture'",
                "} -Launch { Invoke-BoundedProcess -ExecutablePath pwsh -Arguments @('-NoProfile','-Command','Start-Sleep -Milliseconds 200') -TimeoutMilliseconds 10000 -Description game -ProcessLeaseFile $identity } -Clean { Remove-Item -LiteralPath $lease }",
                "@{exitCode=$result.ExitCode;stage=(Get-Content $stageIdentity -Raw | ConvertFrom-Json);game=(Get-Content $identity -Raw | ConvertFrom-Json);leaseExists=(Test-Path $lease)} | ConvertTo-Json -Depth 5 -Compress"
            });
            var run = InvokeSource(source, root);
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            using var json = JsonDocument.Parse(run.StandardOutput);
            Assert.That(json.RootElement.GetProperty("exitCode").GetInt32(), Is.Zero);
            Assert.That(json.RootElement.GetProperty("stage").GetProperty("processId").GetInt32(), Is.Positive);
            Assert.That(json.RootElement.GetProperty("game").GetProperty("processId").GetInt32(), Is.Positive);
            Assert.That(json.RootElement.GetProperty("leaseExists").GetBoolean(), Is.False);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Test]
    public void Ambiguous_existing_catalog_is_rejected_before_new_publication()
    {
        var root = Path.Combine(Path.GetTempPath(), "RimWorldEndToEndRunnerCliTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "fixture-recovery.json"), "{\"schema\":\"RimWorldModdingMcp/E2EFixtureRecovery/v1\",\"groups\":[{\"fixtureLeaseFile\":\"other/fixture.lease.json\",\"processLeaseFile\":\"smoke-001/game-process.json\",\"launchAttempted\":true,\"launchAttempted\":false}]}");
        try
        {
            var source = string.Join(Environment.NewLine, new[]
            {
                "$ErrorActionPreference='Stop'",
                "Import-Module " + PowerShellLiteral(SupportModulePath()) + " -Force",
                "$root=" + PowerShellLiteral(root),
                "$failure=''; try { Invoke-RimWorldEndToEndFixtureLaunch -RunDirectory $root -FixtureLeaseFile (Join-Path $root 'group-002/fixture.lease.json') -ProcessLeaseFile (Join-Path $root 'smoke-002/game-process.json') -Stage { Set-Content (Join-Path $root 'published') 'unsafe' } -Launch {} -Clean {} } catch {$failure=$_.Exception.Message}",
                "@{failure=$failure;published=(Test-Path (Join-Path $root 'published'))} | ConvertTo-Json -Compress"
            });
            var run = InvokeSource(source, root);
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            using var json = JsonDocument.Parse(run.StandardOutput);
            Assert.That(json.RootElement.GetProperty("published").GetBoolean(), Is.False);
            Assert.That(json.RootElement.GetProperty("failure").GetString(), Is.Not.Empty);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [TestCase("stage-failure", false, "stage failed")]
    [TestCase("missing-child", true, "cleanup incomplete")]
    [TestCase("live-child", true, "still running")]
    [TestCase("reused-child", true, "reused")]
    [TestCase("cleanup-failure", true, "storage locked")]
    [TestCase("publisher-live", false, "still running")]
    [TestCase("publisher-missing", false, "cleanup incomplete")]
    public void Fixture_launch_preserves_uncertain_leases_and_primary_failures(string mode, bool retained, string message)
    {
        var root = Path.Combine(Path.GetTempPath(), "RimWorldEndToEndRunnerCliTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var source = string.Join(Environment.NewLine, new[]
            {
                "$ErrorActionPreference='Stop'",
                "Import-Module " + PowerShellLiteral(SupportModulePath()) + " -Force",
                "$root=" + PowerShellLiteral(root) + "; $mode=" + PowerShellLiteral(mode),
                "$lease=Join-Path $root 'group-001/fixture.lease.json'; $identity=Join-Path $root 'smoke-001/game-process.json'",
                "$null=New-Item (Split-Path $lease),(Split-Path $identity) -ItemType Directory -Force",
                "$failure=''; try { Invoke-RimWorldEndToEndFixtureLaunch -ExternalStage:($mode.StartsWith('publisher-')) -RunDirectory $root -FixtureLeaseFile $lease -ProcessLeaseFile $identity -Stage {",
                " if($mode.StartsWith('publisher-')) { if($mode -eq 'publisher-live'){ $p=[Diagnostics.Process]::GetCurrentProcess(); try { @{schema='RimWorldDevGateway/ProcessLease/v1';processId=$PID;processStartUtc=$p.StartTime.ToUniversalTime().ToString('O')} | ConvertTo-Json | Set-Content (Join-Path $root 'group-001/fixture-stage-process.json') } finally {$p.Dispose()} }; throw 'stage failed' }; Set-Content $lease 'owned fixture'; if($mode -in @('stage-failure','cleanup-failure')){throw 'stage failed'}",
                "} -Launch {",
                " if($mode -ne 'missing-child') { $p=[Diagnostics.Process]::GetCurrentProcess(); try { $start=$p.StartTime.ToUniversalTime(); if($mode -eq 'reused-child'){$start=$start.AddSeconds(-1)}; @{schema='RimWorldDevGateway/ProcessLease/v1'; processId=$PID; processStartUtc=$start.ToString('O')} | ConvertTo-Json | Set-Content $identity } finally {$p.Dispose()} }; throw 'launch failed'",
                "} -Clean { if($mode -eq 'cleanup-failure'){throw 'storage locked'}; Remove-Item -LiteralPath $lease } } catch {$failure=$_.Exception.Message}",
                "@{failure=$failure;leaseExists=(Test-Path $lease)} | ConvertTo-Json -Compress"
            });
            var run = InvokeSource(source, root);
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            using var json = JsonDocument.Parse(run.StandardOutput);
            Assert.That(json.RootElement.GetProperty("failure").GetString(), Does.Contain(message));
            Assert.That(json.RootElement.GetProperty("failure").GetString(), Does.Contain(mode is "stage-failure" or "cleanup-failure" || mode.StartsWith("publisher-", StringComparison.Ordinal) ? "stage failed" : "launch failed"));
            Assert.That(json.RootElement.GetProperty("leaseExists").GetBoolean(), Is.EqualTo(retained));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Test]
    public void Fixture_launch_persists_intent_before_stage_and_cleans_after_exact_child_exit()
    {
        var root = Path.Combine(Path.GetTempPath(), "RimWorldEndToEndRunnerCliTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var source = string.Join(Environment.NewLine, new[]
            {
                "$ErrorActionPreference='Stop'",
                "Import-Module " + PowerShellLiteral(SupportModulePath()) + " -Force",
                "$root=" + PowerShellLiteral(root),
                "$lease=Join-Path $root 'group-001/fixture.lease.json'; $identity=Join-Path $root 'smoke-001/game-process.json'",
                "$null=New-Item (Split-Path $lease),(Split-Path $identity) -ItemType Directory -Force",
                "$result=Invoke-RimWorldEndToEndFixtureLaunch -RunDirectory $root -FixtureLeaseFile $lease -ProcessLeaseFile $identity -Stage {",
                " $catalog=Get-Content (Join-Path $root 'fixture-recovery.json') -Raw | ConvertFrom-Json; if($catalog.groups[0].launchAttempted){throw 'early launch'}; Set-Content $lease 'owned fixture'",
                "} -Launch {",
                " $catalog=Get-Content (Join-Path $root 'fixture-recovery.json') -Raw | ConvertFrom-Json; if(-not $catalog.groups[0].launchAttempted){throw 'missing intent'}",
                " $info=[Diagnostics.ProcessStartInfo]::new('pwsh'); $info.UseShellExecute=$false; $info.CreateNoWindow=$true; foreach($arg in @('-NoProfile','-Command','Start-Sleep -Milliseconds 500')){$info.ArgumentList.Add($arg)}",
                " $child=[Diagnostics.Process]::Start($info); try { @{schema='RimWorldDevGateway/ProcessLease/v1';processId=$child.Id;processStartUtc=$child.StartTime.ToUniversalTime().ToString('O')} | ConvertTo-Json | Set-Content $identity; if(-not $child.WaitForExit(10000)){throw 'child timeout'} } finally {$child.Dispose()}; 'native-result'",
                "} -Clean { Remove-Item -LiteralPath $lease }",
                "@{result=$result;leaseExists=(Test-Path $lease)} | ConvertTo-Json -Compress"
            });
            var run = InvokeSource(source, root);
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            using var json = JsonDocument.Parse(run.StandardOutput);
            Assert.That(json.RootElement.GetProperty("result").GetString(), Is.EqualTo("native-result"));
            Assert.That(json.RootElement.GetProperty("leaseExists").GetBoolean(), Is.False);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Test]
    public void Owned_run_records_exact_runner_identity_before_fixture_work()
    {
        var root = Path.Combine(Path.GetTempPath(), "RimWorldEndToEndRunnerCliTests", Guid.NewGuid().ToString("N"));
        var runDirectory = Path.Combine(root, "artifacts", "EndToEndRuns", "Grouped", "owned-run");
        Directory.CreateDirectory(runDirectory);
        File.WriteAllText(Path.Combine(runDirectory, "typed-run.json"), JsonSerializer.Serialize(new
        { schema = "RimWorldModdingMcp/OwnedE2ERun/v1", runDirectory }));
        try
        {
            var source = string.Join(Environment.NewLine, new[]
            {
                "$ErrorActionPreference = 'Stop'",
                "Import-Module " + PowerShellLiteral(SupportModulePath()) + " -Force",
                "$path = Initialize-RimWorldEndToEndOwnedRun -RepositoryRoot " + PowerShellLiteral(root) +
                    " -OwnedRunDirectory " + PowerShellLiteral(runDirectory),
                "[pscustomobject]@{ path=$path; pid=$PID; receipt=(Get-Content -LiteralPath (Join-Path $path 'runner-process.json') -Raw | ConvertFrom-Json) } | ConvertTo-Json -Compress"
            });
            var run = InvokeSource(source, root);
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            using var json = JsonDocument.Parse(run.StandardOutput);
            Assert.That(json.RootElement.GetProperty("path").GetString(), Is.EqualTo(runDirectory));
            var receipt = json.RootElement.GetProperty("receipt");
            Assert.That(receipt.GetProperty("schema").GetString(), Is.EqualTo("RimWorldDevGateway/ProcessLease/v1"));
            Assert.That(receipt.GetProperty("processId").GetInt32(), Is.EqualTo(json.RootElement.GetProperty("pid").GetInt32()));
            Assert.That(DateTimeOffset.TryParse(receipt.GetProperty("processStartUtc").GetString(), out _), Is.True);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Test]
    public void Plan_and_stage_forward_exact_selection_to_host_package_preflight()
    {
        var temporaryRoot = Path.Combine(Path.GetTempPath(), "RimWorldEndToEndRunnerCliTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var source = string.Join(Environment.NewLine, new[]
            {
                "$ErrorActionPreference = 'Stop'",
                "$tokens = $null; $errors = $null",
                "$ast = [System.Management.Automation.Language.Parser]::ParseFile(" + PowerShellLiteral(RunnerPath()) + ", [ref]$tokens, [ref]$errors)",
                "$function = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Invoke-HostTool' }, $true)",
                ". ([scriptblock]::Create($function.Extent.Text))",
                "function Invoke-BoundedProcess { param($ExecutablePath, $Arguments, $TimeoutMilliseconds, $Description) [pscustomobject]@{ Arguments = @($Arguments) } }",
                "$resolvedRimWorldPath = $hostProject = $repositoryRoot = $modsRoot = " + PowerShellLiteral(temporaryRoot),
                "$rimWorldMajorVersion = '1.7'; $packageIds = @('ludeon.rimworld', 'alpha.mod')",
                "$ProjectPath = @(); $leaseFile = 'test-lease.json'",
                "$testIdFilters = @('alpha.base-a', 'alpha.base-b'); $GroupId = @('brrainz.harmony|ludeon.rimworld|alpha.mod')",
                "$calls = @($ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.CommandAst] -and $node.GetCommandName() -eq 'Invoke-HostTool' -and $node.Extent.Text -match \"-Operation '(plan|stage)'\" }, $true))",
                "@($calls | ForEach-Object { & ([scriptblock]::Create($_.Extent.Text)) }) | ConvertTo-Json -Depth 5 -Compress"
            });
            var run = InvokeSource(source, temporaryRoot);
            Assert.That(run.ExitCode, Is.Zero, run.StandardError);
            using var json = JsonDocument.Parse(run.StandardOutput);
            Assert.That(json.RootElement.GetArrayLength(), Is.EqualTo(2));
            foreach (var call in json.RootElement.EnumerateArray())
            {
                var arguments = call.GetProperty("Arguments").EnumerateArray().Select(value => value.GetString()).ToArray();
                Assert.That(arguments.Count(value => value == "--test-id"), Is.EqualTo(2));
                Assert.That(arguments.Count(value => value == "--group-id"), Is.EqualTo(1));
                Assert.That(arguments, Does.Contain("alpha.base-a").And.Contain("alpha.base-b")
                    .And.Contain("brrainz.harmony|ludeon.rimworld|alpha.mod"));
            }
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Test]
    public void Runner_composes_exact_group_launches_and_guaranteed_stage_cleanup()
    {
        var source = File.ReadAllText(RunnerPath());
        var support = File.ReadAllText(SupportModulePath());

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("[switch]$DryRun"));
            Assert.That(source, Does.Contain("foreach ($group in $selectedGroups)"));
            Assert.That(source, Does.Contain("'-RunEndToEndTests'"));
            Assert.That(source, Does.Contain("'-SkipBuildDeploy'"));
            Assert.That(source, Does.Contain("'-AdditionalModIdsFile'"));
            Assert.That(source, Does.Contain("[string[]]$TestId"));
            Assert.That(source, Does.Contain("'-EndToEndTestIds'"));
            Assert.That(source, Does.Contain("[string]$Language = 'English'"));
            Assert.That(source, Does.Contain("'-Language', $Language"));
            Assert.That(source, Does.Contain("[System.IO.File]::WriteAllLines"));
            Assert.That(source, Does.Contain("Join-Path $runDirectory ('smoke-{0:D3}' -f $groupIndex)"));
            Assert.That(source, Does.Contain("finally"));
            Assert.That(source, Does.Contain("'clean'"));
            Assert.That(source, Does.Contain("aggregate.json"));
            Assert.That(source, Does.Contain("results.junit.xml"));
            Assert.That(source, Does.Contain("product-deployment-evidence.json"));
            Assert.That(source, Does.Contain("Get-RimWorldDeployedProductEvidence"));
            Assert.That(source, Does.Contain("ProductDeploymentEvidence"));
            Assert.That(source, Does.Contain("ConvertTo-RimWorldProductEvidenceJson"));
            Assert.That(support, Does.Contain("[System.IO.FileAttributes]::ReparsePoint"));
            Assert.That(support, Does.Contain("Get-ChildItem -LiteralPath $packageRoot -Recurse -Force -ErrorAction Stop"));
            Assert.That(support, Does.Contain("-LiteralPath $file.FullName").And.Contain("-ErrorAction Stop"));
        });
    }

    [Test]
    public void Smoke_launch_projection_defaults_to_muted_and_forwards_explicit_audio_opt_in()
    {
        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "RimWorldEndToEndRunnerCliTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var invocation = string.Join(Environment.NewLine, new[]
            {
                $"Import-Module {PowerShellLiteral(SupportModulePath())} -Force",
                "$default = New-RimWorldEndToEndSmokeLaunch -Arguments @('-File', 'smoke.ps1')",
                "$enabled = New-RimWorldEndToEndSmokeLaunch -Arguments @('-File', 'smoke.ps1') -EnableAudio",
                "[pscustomobject]@{ " +
                "Default = [pscustomobject]@{ AudioEnabled = $default.AudioEnabled; EnableAudioArgumentCount = @($default.Arguments | Where-Object { $_ -ceq '-EnableAudio' }).Count }; " +
                "Enabled = [pscustomobject]@{ AudioEnabled = $enabled.AudioEnabled; EnableAudioArgumentCount = @($enabled.Arguments | Where-Object { $_ -ceq '-EnableAudio' }).Count; Arguments = @($enabled.Arguments) } " +
                "} | ConvertTo-Json -Depth 4 -Compress"
            });

            var run = InvokeSource(invocation, temporaryRoot);

            Assert.Multiple(() =>
            {
                Assert.That(run.ExitCode, Is.EqualTo(0), run.StandardError);
                Assert.That(run.StandardOutput, Does.Contain("\"AudioEnabled\":false"));
                Assert.That(run.StandardOutput, Does.Contain("\"AudioEnabled\":true"));
                Assert.That(run.StandardOutput, Does.Contain("\"EnableAudioArgumentCount\":0"));
                Assert.That(run.StandardOutput, Does.Contain("\"EnableAudioArgumentCount\":1"));
                Assert.That(run.StandardOutput, Does.Contain("-EnableAudio"));
            });
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Test]
    public void Unsafe_language_is_rejected_by_parameter_binding_before_game_path_validation()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var run = Invoke("-DryRun", "-Language", "../German", "-RimWorldPath", missing);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.Not.Zero);
            Assert.That(run.StandardError, Does.Contain("Language"));
            Assert.That(run.StandardError, Does.Not.Contain("RimWorld path does not exist"));
        });
    }

    [Test]
    public void Missing_game_path_is_invalid_usage_before_any_mutation()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var run = Invoke("-DryRun", "-RimWorldPath", missing);

        Assert.Multiple(() =>
        {
            Assert.That(run.ExitCode, Is.EqualTo(2));
            Assert.That(run.StandardError, Does.Contain("RimWorld path does not exist"));
        });
    }

    [Test]
    public void Junit_writer_replaces_xml_invalid_terminal_controls_and_preserves_unicode()
    {
        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "RimWorldEndToEndRunnerCliTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var errorPath = Path.Combine(temporaryRoot, "stderr.txt");
            var junitPath = Path.Combine(temporaryRoot, "result.xml");
            File.WriteAllText(
                errorPath,
                "red \u001b[31m failure \ud83e\uddc0 \0 done",
                new UTF8Encoding(false));
            var invocation = string.Join(Environment.NewLine, new[]
            {
                $"Import-Module {PowerShellLiteral(SupportModulePath())} -Force",
                "$group = [pscustomobject]@{",
                "  GroupId = 'ludeon.rimworld|example.mod'",
                "  Status = 'failed'",
                "  Message = \"failed $([char]0x1b) visibly\"",
                $"  StandardError = {PowerShellLiteral(errorPath)}",
                "}",
                $"Write-RimWorldEndToEndJUnitReport -Path {PowerShellLiteral(junitPath)} -GroupResults @($group)"
            });

            var run = InvokeSource(invocation, temporaryRoot);
            var xml = File.ReadAllText(junitPath);
            var document = System.Xml.Linq.XDocument.Parse(xml);
            var failure = document.Descendants("failure").Single();

            Assert.Multiple(() =>
            {
                Assert.That(run.ExitCode, Is.EqualTo(0), run.StandardError);
                Assert.That(xml.IndexOfAny(new[] { '\u001b', '\0' }), Is.EqualTo(-1));
                Assert.That(failure.Attribute("message")!.Value, Is.EqualTo("failed \ufffd visibly"));
                Assert.That(failure.Value, Does.Contain("red \ufffd[31m failure \ud83e\uddc0 \ufffd done"));
            });
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Test]
    public void Deployment_evidence_hashes_the_exact_product_package_and_excludes_test_staging()
    {
        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "RimWorldEndToEndRunnerCliTests",
            Guid.NewGuid().ToString("N"));
        var game = Path.Combine(temporaryRoot, "Game");
        var package = Path.Combine(game, "Mods", "example.product");
        var assembly = Path.Combine(package, "1.6", "Assemblies", "Example.dll");
        var texture = Path.Combine(package, "1.6", "Textures", "Example.png");
        var stagedTest = Path.Combine(package, "1.6", "DevEndToEndTests", "Example.Tests.dll");
        var project = Path.Combine(temporaryRoot, "Example.csproj");
        var buildLog = Path.Combine(temporaryRoot, "build.log");
        Directory.CreateDirectory(Path.GetDirectoryName(assembly)!);
        Directory.CreateDirectory(Path.GetDirectoryName(texture)!);
        Directory.CreateDirectory(Path.GetDirectoryName(stagedTest)!);
        File.WriteAllText(assembly, "assembly", new UTF8Encoding(false));
        File.WriteAllText(texture, "texture", new UTF8Encoding(false));
        File.WriteAllText(stagedTest, "test-only", new UTF8Encoding(false));
        File.WriteAllText(project, "<Project />", new UTF8Encoding(false));
        File.WriteAllText(buildLog, "build", new UTF8Encoding(false));
        try
        {
            var invocation = string.Join(Environment.NewLine, new[]
            {
                $"Import-Module {PowerShellLiteral(SupportModulePath())} -Force",
                "$evidence = Get-RimWorldDeployedProductEvidence " +
                "-PackageId 'example.product' " +
                $"-ProjectPath {PowerShellLiteral(project)} " +
                $"-RimWorldPath {PowerShellLiteral(game)} " +
                $"-BuildLogPath {PowerShellLiteral(buildLog)}",
                "$evidence | ConvertTo-Json -Depth 8 -Compress"
            });
            var run = InvokeSource(invocation, temporaryRoot);

            Assert.That(run.ExitCode, Is.EqualTo(0), run.StandardError);
            Assert.Multiple(() =>
            {
                Assert.That(run.StandardOutput, Does.Contain("\"PackageId\":\"example.product\""));
                Assert.That(run.StandardOutput, Does.Contain("1.6/Assemblies/Example.dll"));
                Assert.That(run.StandardOutput, Does.Contain("1.6/Textures/Example.png"));
                Assert.That(run.StandardOutput, Does.Contain(HashFile(assembly)));
                Assert.That(run.StandardOutput, Does.Contain(HashFile(texture)));
                Assert.That(run.StandardOutput, Does.Not.Contain("DevEndToEndTests"));
            });
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Test]
    public void Product_evidence_json_keeps_an_array_root_for_one_or_two_products()
    {
        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "RimWorldEndToEndRunnerCliTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var invocation = string.Join(Environment.NewLine, new[]
            {
                $"Import-Module {PowerShellLiteral(SupportModulePath())} -Force",
                "ConvertTo-RimWorldProductEvidenceJson -Evidence @([pscustomobject]@{ PackageId = 'one' })",
                "ConvertTo-RimWorldProductEvidenceJson -Evidence @(" +
                "[pscustomobject]@{ PackageId = 'one' }, " +
                "[pscustomobject]@{ PackageId = 'two' })"
            });
            var run = InvokeSource(invocation, temporaryRoot);
            var lines = run.StandardOutput
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Multiple(() =>
            {
                Assert.That(run.ExitCode, Is.EqualTo(0), run.StandardError);
                Assert.That(lines, Has.Length.EqualTo(2));
                Assert.That(lines[0], Is.EqualTo("[{\"PackageId\":\"one\"}]"));
                Assert.That(lines[1], Is.EqualTo(
                    "[{\"PackageId\":\"one\"},{\"PackageId\":\"two\"}]"));
            });
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    private static InvocationResult Invoke(params string[] arguments)
    {
        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "RimWorldEndToEndRunnerCliTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            var invocationPath = Path.Combine(temporaryRoot, "invoke.ps1");
            var escapedArguments = string.Join(" ", Array.ConvertAll(
                arguments,
                argument => argument.StartsWith("-", StringComparison.Ordinal)
                    ? argument
                    : PowerShellLiteral(argument)));
            var invocation =
                "$ErrorActionPreference = 'Stop'" + Environment.NewLine +
                $"& {PowerShellLiteral(RunnerPath())} {escapedArguments}" + Environment.NewLine +
                "exit $LASTEXITCODE" + Environment.NewLine;
            File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));
            var startInfo = new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{invocationPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start pwsh.");
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000))
            {
                process.Kill();
                throw new TimeoutException("E2E runner validation probe timed out.");
            }

            Task.WaitAll(output, error);
            return new InvocationResult(process.ExitCode, output.Result, error.Result);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    private static InvocationResult InvokeSource(string source, string temporaryRoot)
    {
        var invocationPath = Path.Combine(temporaryRoot, "support-probe.ps1");
        File.WriteAllText(
            invocationPath,
            source + Environment.NewLine + "exit $LASTEXITCODE" + Environment.NewLine,
            new UTF8Encoding(false));
        var startInfo = new ProcessStartInfo
        {
            FileName = "pwsh.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{invocationPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start pwsh.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000))
        {
            process.Kill();
            throw new TimeoutException("E2E runner support probe timed out.");
        }

        Task.WaitAll(output, error);
        return new InvocationResult(process.ExitCode, output.Result, error.Result);
    }

    private static string PowerShellLiteral(string value) => $"'{value.Replace("'", "''")}'";

    private static string HashFile(string path)
    {
        using var algorithm = SHA256.Create();
        using var stream = File.OpenRead(path);
        return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
    }

    private static string SupportModulePath() => Path.Combine(
        FindSourceRepositoryRoot(),
        "scripts",
        "RimWorldEndToEndRunner.Support.psm1");

    private static string RunnerPath() => Path.Combine(
        FindSourceRepositoryRoot(),
        "scripts",
        "Invoke-RimWorldEndToEndTests.ps1");

    private static string FindSourceRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "scripts", "Invoke-GatewaySmoke.ps1")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not find the source repository root.");
    }

    private sealed class InvocationResult
    {
        public InvocationResult(int exitCode, string standardOutput, string standardError)
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
