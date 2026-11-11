using System.Diagnostics;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using RimWorldDevGateway.EndToEndHost;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class FixtureDiagnosticAccountingTests
{
    private const string Message = "Type DeliberatelyMissingDefType is not a Def type or could not be found, in file Diagnostic.xml. Context: <DeliberatelyMissingDefType/>";

    [Test]
    public void Intermediate_scan_reads_with_open_stdout_writer_and_final_keeps_late_errors()
    {
        using var sandbox = new Sandbox();
        var receipt = sandbox.WriteReceipt();
        string invocation = $"Import-Module {Literal(Path.Combine(FindRoot(), "scripts", "RimWorldFixtureDiagnostics.Support.psm1"))} -Force\n" +
            $"$expectations = Read-RimWorldFixtureExpectations -Path {Literal(receipt.Path)} -Sha256 '{receipt.Sha256}' -ActivePackageIds @('ludeon.rimworld','fumblesneeze.owner','fumblesneeze.fixture','fumblesneeze.rimworlddevgateway')\n";
        using (var writer = new StreamWriter(new FileStream(sandbox.Log, FileMode.Create, FileAccess.Write, FileShare.Read), new UTF8Encoding(false)))
        {
            writer.Write("21:03:07 ERROR " + Message + "\n21:03:07    at Verse.Log.Error(String text)\n");
            writer.Flush();
            using var initial = JsonDocument.Parse(sandbox.Invoke(invocation +
                $"Get-RimWorldFixtureLogAccounting -Path {Literal(sandbox.Log)} -Expectations $expectations | ConvertTo-Json -Depth 15\n"));
            Assert.That(initial.RootElement.GetProperty("Passed").GetBoolean(), Is.True);
            Assert.That(initial.RootElement.GetProperty("Expected")[0].GetProperty("ObservedCount").GetInt32(), Is.EqualTo(1));
            writer.Write("21:03:08 ERROR Unexpected appended error\n");
            writer.Flush();
            using var appended = JsonDocument.Parse(sandbox.Invoke(invocation +
                $"Get-RimWorldFixtureLogAccounting -Path {Literal(sandbox.Log)} -Expectations $expectations | ConvertTo-Json -Depth 15\n"));
            Assert.That(appended.RootElement.GetProperty("Passed").GetBoolean(), Is.False);
            Assert.That(appended.RootElement.GetProperty("Unexpected").GetArrayLength(), Is.EqualTo(1));
        }
        using var final = JsonDocument.Parse(sandbox.Invoke(invocation +
            $"Get-RimWorldFixtureLogAccounting -Path {Literal(sandbox.Log)} -Expectations $expectations -Final | ConvertTo-Json -Depth 15\n"));
        Assert.That(final.RootElement.GetProperty("Passed").GetBoolean(), Is.False);
        Assert.That(final.RootElement.GetProperty("Unexpected").GetArrayLength(), Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Intermediate_defers_only_unterminated_tail_and_final_validates_it(bool hasCompleteError)
    {
        using var sandbox = new Sandbox();
        string prefix = hasCompleteError ? "21:03:07 ERROR Complete error\n" : "21:03:07 INFO Ready\n";
        const string partial = "21:03:08 ERROR Partial diagnostic";
        string invocation = $"Import-Module {Literal(Path.Combine(FindRoot(), "scripts", "RimWorldFixtureDiagnostics.Support.psm1"))} -Force\n";
        using (var writer = new StreamWriter(new FileStream(sandbox.Log, FileMode.Create, FileAccess.Write, FileShare.Read), new UTF8Encoding(false)))
        {
            writer.Write(prefix + partial);
            writer.Flush();
            using var intermediate = JsonDocument.Parse(sandbox.Invoke(invocation +
                $"Get-RimWorldFixtureLogAccounting -Path {Literal(sandbox.Log)} | ConvertTo-Json -Depth 15\n"));
            Assert.That(intermediate.RootElement.GetProperty("Passed").GetBoolean(), Is.EqualTo(!hasCompleteError));
            Assert.That(intermediate.RootElement.GetProperty("Unexpected").GetArrayLength(), Is.EqualTo(hasCompleteError ? 1 : 0));
            Assert.That(intermediate.RootElement.GetProperty("SnapshotByteLength").GetInt64(), Is.EqualTo(Encoding.UTF8.GetByteCount(prefix + partial)));
            Assert.That(intermediate.RootElement.GetProperty("DeferredTrailingCharacters").GetInt32(), Is.EqualTo(partial.Length));
            var rejected = sandbox.Invoke(invocation +
                $"try {{ $null = Get-RimWorldFixtureLogAccounting -Path {Literal(sandbox.Log)} -Final; throw 'writer accepted' }} catch {{ if ($_.Exception.Message -eq 'writer accepted') {{ throw }}; 'writer correctly rejected' }}\n");
            Assert.That(rejected.Trim(), Is.EqualTo("writer correctly rejected"));
        }
        using var final = JsonDocument.Parse(sandbox.Invoke(invocation +
            $"Get-RimWorldFixtureLogAccounting -Path {Literal(sandbox.Log)} -Final | ConvertTo-Json -Depth 15\n"));
        Assert.That(final.RootElement.GetProperty("Passed").GetBoolean(), Is.False);
        Assert.That(final.RootElement.GetProperty("Unexpected").GetArrayLength(), Is.EqualTo(hasCompleteError ? 2 : 1));
        Assert.That(final.RootElement.GetProperty("DeferredTrailingCharacters").GetInt32(), Is.Zero);
    }

    [Test]
    public void Actual_smoke_active_mod_projection_accepts_selected_fixture_receipt()
    {
        using var sandbox = new Sandbox();
        var receipt = sandbox.WriteReceipt(includeHarmony: true);
        string smoke = Path.Combine(FindRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
        var output = sandbox.Invoke($"Import-Module {Literal(Path.Combine(FindRoot(), "scripts", "RimWorldFixtureDiagnostics.Support.psm1"))} -Force\n" +
            "$tokens=$null; $errors=$null\n" +
            $"$ast = [Management.Automation.Language.Parser]::ParseFile({Literal(smoke)}, [ref]$tokens, [ref]$errors)\n" +
            "$function = $ast.Find({ param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq 'Get-GatewayActiveModIds' }, $true)\nInvoke-Expression $function.Extent.Text\n" +
            "$active = @(Get-GatewayActiveModIds -AdditionalPackageIds @('brrainz.harmony','fumblesneeze.owner','fumblesneeze.fixture'))\n" +
            $"Read-RimWorldFixtureExpectations -Path {Literal(receipt.Path)} -Sha256 '{receipt.Sha256}' -ActivePackageIds $active | ConvertTo-Json -Depth 8\n");
        using var result = JsonDocument.Parse(output);
        Assert.That(result.RootElement.GetProperty("Errors").GetArrayLength(), Is.EqualTo(1));
    }

    [Test]
    public void Native_padded_warning_and_info_headers_end_the_expected_error_block()
    {
        using var sandbox = new Sandbox();
        var receipt = sandbox.WriteReceipt();
        // Exact padding/trace shapes observed in native Player.log at lines 545, 559 and 580.
        File.WriteAllText(sandbox.Log, "21:03:07 ERROR " + Message + "\n" +
            "21:03:07    at Verse.DirectXmlToObjectNew.DefFromNodeNew()\n" +
            "INFO copied diagnostic context\n" +
            "21:03:09  WARN [RimWorldDevGateway] API active on authenticated IPv4 loopback port 56785\n" +
            "21:03:09    at Verse.Log.Warning(String text) [0x19]\n" +
            "21:03:23  INFO Initializing new game with mods:\n  - fumblesneeze.fixture\n");
        var output = sandbox.Invoke($"Import-Module {Literal(Path.Combine(FindRoot(), "scripts", "RimWorldFixtureDiagnostics.Support.psm1"))} -Force\n" +
            $"$expectations = Read-RimWorldFixtureExpectations -Path {Literal(receipt.Path)} -Sha256 '{receipt.Sha256}' -ActivePackageIds @('ludeon.rimworld','fumblesneeze.owner','fumblesneeze.fixture','fumblesneeze.rimworlddevgateway')\n" +
            $"Get-RimWorldFixtureLogAccounting -Path {Literal(sandbox.Log)} -Expectations $expectations -Final | ConvertTo-Json -Depth 15\n");
        using var result = JsonDocument.Parse(output);
        Assert.That(result.RootElement.GetProperty("Passed").GetBoolean(), Is.True, output);
        var lines = result.RootElement.GetProperty("Expected")[0].GetProperty("Matches")[0].GetProperty("Lines");
        Assert.That(lines.EnumerateArray().Select(value => value.GetString()), Is.EqualTo(new[] {
            "21:03:07 ERROR " + Message,
            "21:03:07    at Verse.DirectXmlToObjectNew.DefFromNodeNew()",
            "INFO copied diagnostic context"
        }), "A matched error must not absorb later records or their traces.");
    }

    [Test]
    public void No_selected_expectations_grant_no_error_allowance()
    {
        using var sandbox = new Sandbox();
        File.WriteAllText(sandbox.Log, "21:03:07 ERROR " + Message + "\n");
        var output = sandbox.Invoke($"Import-Module {Literal(Path.Combine(FindRoot(), "scripts", "RimWorldFixtureDiagnostics.Support.psm1"))} -Force\n" +
            $"Get-RimWorldFixtureLogAccounting -Path {Literal(sandbox.Log)} -Final | ConvertTo-Json -Depth 12\n");
        using var result = JsonDocument.Parse(output);
        Assert.That(result.RootElement.GetProperty("Passed").GetBoolean(), Is.False);
        Assert.That(result.RootElement.GetProperty("Expected").GetArrayLength(), Is.Zero);
        Assert.That(result.RootElement.GetProperty("Unexpected").GetArrayLength(), Is.EqualTo(1));
    }

    [Test]
    public void Selected_test_can_expect_one_exact_native_warning_without_hiding_its_exception()
    {
        using var sandbox = new Sandbox();
        const string message = "Deliberate runtime failure: System.InvalidOperationException: owned fixture fault";
        File.WriteAllText(sandbox.Log, "21:03:07 WARN " + message + "\n21:03:07    at OwnedFixture.Fail()\n");
        string expectations = "$expectations = [pscustomobject]@{ File='selected-test-expectations.json'; Sha256='" + new string('A', 64) +
            "'; Errors=@([pscustomobject]@{OwnerPackageId='fumblesneeze.owner'; PackageId=''; ManifestSha256='" + new string('B', 64) +
            "'; Severity='WARN'; Message=" + Literal(message) + "; Count=1}) }\n";
        var output = sandbox.Invoke($"Import-Module {Literal(Path.Combine(FindRoot(), "scripts", "RimWorldFixtureDiagnostics.Support.psm1"))} -Force\n" +
            expectations + $"Get-RimWorldFixtureLogAccounting -Path {Literal(sandbox.Log)} -Expectations $expectations -Final | ConvertTo-Json -Depth 12\n");
        using var result = JsonDocument.Parse(output);
        Assert.That(result.RootElement.GetProperty("Passed").GetBoolean(), Is.True, output);
        Assert.That(result.RootElement.GetProperty("Expected")[0].GetProperty("ObservedCount").GetInt32(), Is.EqualTo(1));
        Assert.That(result.RootElement.GetProperty("Expected")[0].GetProperty("Matches")[0].GetProperty("Lines").GetArrayLength(), Is.EqualTo(2));
        Assert.That(result.RootElement.GetProperty("Unexpected").GetArrayLength(), Is.Zero);
    }

    [Test]
    public void Expectation_receipt_is_create_new_and_cannot_overwrite_an_existing_handoff()
    {
        using var sandbox = new Sandbox();
        var receipt = sandbox.WriteReceipt();
        string original = File.ReadAllText(receipt.Path);
        Assert.Throws<IOException>(() => sandbox.WriteReceipt());
        Assert.That(File.ReadAllText(receipt.Path), Is.EqualTo(original));
    }

    [Test]
    public void Selected_test_binds_an_exact_inner_cause_to_its_declared_parent()
    {
        using var sandbox = new Sandbox();
        const string message = "Exception while saving game: System.InvalidOperationException: Owned state was not preserved.";
        const string cause = " ---> System.InvalidOperationException: Expected S138 native-save fault fixture";
        File.WriteAllText(sandbox.Log, "21:03:07 ERROR " + message + "\n" + cause + "\n   at OwnedFixture.Fail()\n");
        string expectations = "$expectations = [pscustomobject]@{ File='selected-test-expectations.json'; Sha256='" + new string('A', 64) +
            "'; Errors=@([pscustomobject]@{OwnerPackageId='fumblesneeze.owner'; PackageId=''; ManifestSha256='" + new string('B', 64) +
            "'; Severity='ERROR'; Message=" + Literal(message) + "; Count=1; InnerCauses=@(" + Literal(cause) + ")}) }\n";
        var output = sandbox.Invoke($"Import-Module {Literal(Path.Combine(FindRoot(), "scripts", "RimWorldFixtureDiagnostics.Support.psm1"))} -Force\n" +
            expectations + $"Get-RimWorldFixtureLogAccounting -Path {Literal(sandbox.Log)} -Expectations $expectations -Final | ConvertTo-Json -Depth 12\n");
        using var result = JsonDocument.Parse(output);
        Assert.That(result.RootElement.GetProperty("Passed").GetBoolean(), Is.True, output);
        Assert.That(result.RootElement.GetProperty("Expected")[0].GetProperty("Matches")[0].GetProperty("Lines").GetArrayLength(), Is.EqualTo(3));
        Assert.That(result.RootElement.GetProperty("Unexpected").GetArrayLength(), Is.Zero);
    }

    [TestCase("wrong-severity")]
    [TestCase("missing-parent")]
    [TestCase("extra-parent")]
    [TestCase("missing-cause")]
    [TestCase("changed-cause")]
    [TestCase("extra-cause")]
    [TestCase("other-parent")]
    [TestCase("trace-before-cause")]
    [TestCase("adjacent-error")]
    public void Runtime_expectation_does_not_hide_different_or_adjacent_diagnostics(string mode)
    {
        using var sandbox = new Sandbox();
        const string message = "Owned failure: System.InvalidOperationException: planned";
        const string cause = " ---> System.InvalidOperationException: precise inner cause";
        string record = "21:03:07 ERROR " + message + "\n" + cause + "\n   at OwnedFixture.Fail()\n";
        string log = mode switch
        {
            "wrong-severity" => record.Replace(" ERROR ", " WARN "),
            "missing-parent" => "21:03:07 INFO Ready\n",
            "extra-parent" => record + record,
            "missing-cause" => record.Replace(cause + "\n", ""),
            "changed-cause" => record.Replace("precise inner cause", "other cause"),
            "extra-cause" => record + cause + "\n",
            "other-parent" => record.Replace("Owned failure:", "Foreign failure:"),
            "trace-before-cause" => record.Replace(cause, "   at Unrelated.Trace()\n" + cause),
            "adjacent-error" => record + "21:03:08 ERROR Independent failure\n",
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
        File.WriteAllText(sandbox.Log, log);
        string expectations = "$expectations = [pscustomobject]@{ File='selected-test-expectations.json'; Sha256='" + new string('A', 64) +
            "'; Errors=@([pscustomobject]@{OwnerPackageId='fumblesneeze.owner'; PackageId=''; ManifestSha256='" + new string('B', 64) +
            "'; Severity='ERROR'; Message=" + Literal(message) + "; Count=1; InnerCauses=@(" + Literal(cause) + ")}) }\n";
        var output = sandbox.Invoke($"Import-Module {Literal(Path.Combine(FindRoot(), "scripts", "RimWorldFixtureDiagnostics.Support.psm1"))} -Force\n" +
            expectations + $"Get-RimWorldFixtureLogAccounting -Path {Literal(sandbox.Log)} -Expectations $expectations -Final | ConvertTo-Json -Depth 12\n");
        using var result = JsonDocument.Parse(output);
        Assert.That(result.RootElement.GetProperty("Passed").GetBoolean(), Is.False, output);
        Assert.That(result.RootElement.GetProperty("Unexpected").GetArrayLength() +
            result.RootElement.GetProperty("CountMismatches").GetArrayLength() +
            result.RootElement.GetProperty("InnerCauseMismatches").GetArrayLength(), Is.GreaterThan(0));
    }

    [TestCase("exact", true, 1)]
    [TestCase("missing-final", false, 0)]
    [TestCase("missing-startup", true, 0)]
    [TestCase("extra", false, 2)]
    [TestCase("unrelated", false, 1)]
    [TestCase("changed-context", false, 0)]
    [TestCase("embedded", false, 0)]
    [TestCase("legacy", true, 1)]
    [TestCase("fatal", false, 0)]
    [TestCase("warning", false, 0)]
    [TestCase("info-continuation", false, 0)]
    [TestCase("warning-continuation", false, 0)]
    [TestCase("timestamped-continuation", false, 0)]
    [TestCase("padded-info-continuation", false, 0)]
    [TestCase("padded-warning-continuation", false, 0)]
    [TestCase("leading-message-space", false, 0)]
    [TestCase("info-error-continuation", false, 0)]
    [TestCase("warning-error-continuation", false, 0)]
    [TestCase("legacy-severity", true, 1)]
    public void Planned_fixture_receipt_reaches_real_smoke_accounting(string mode, bool passes, int matchedCount)
    {
        using var sandbox = new Sandbox();
        var receipt = sandbox.WriteReceipt();
        string entry = "21:03:07 ERROR " + Message + "\n21:03:07    at Verse.DirectXmlToObjectNew.DefFromNodeNew()\n";
        string log = mode switch
        {
            "missing-final" or "missing-startup" => "21:03:07 INFO startup\n",
            "extra" => entry + entry,
            "unrelated" => entry + "21:03:08 ERROR System.TypeLoadException: unrelated\n",
            "changed-context" => entry.Replace("<DeliberatelyMissingDefType/>", "<AnotherDef/>"),
            "embedded" => "21:03:07 ERROR Other failure mentions " + Message + "\n",
            "legacy" => Message + "\nVerse.Log:Error(string)\n",
            "fatal" => entry.Replace(" ERROR ", " FATAL "),
            "warning" => entry.Replace(" ERROR ", " WARNING "),
            "info-continuation" => "21:03:07 INFO explanation\n" + Message + "\n",
            "warning-continuation" => "21:03:07 WARN explanation\n" + Message + "\n",
            "timestamped-continuation" => "21:03:07 INFO explanation\n21:03:07 " + Message + "\n",
            "padded-info-continuation" => "21:03:07  INFO explanation\n" + Message + "\n",
            "padded-warning-continuation" => "21:03:07  WARN explanation\n" + Message + "\n",
            "leading-message-space" => entry.Replace(" ERROR ", " ERROR  "),
            "info-error-continuation" => "21:03:07  INFO explanation\nERROR " + Message + "\n    at Verse.Log.Error(String text)\n",
            "warning-error-continuation" => "21:03:07  WARN explanation\nERROR " + Message + "\n    at Verse.Log.Error(String text)\n",
            "legacy-severity" => "ERROR " + Message + "\nVerse.Log:Error(string)\n",
            _ => entry
        };
        File.WriteAllText(sandbox.Log, log);
        string module = Path.Combine(FindRoot(), "scripts", "RimWorldFixtureDiagnostics.Support.psm1");
        var output = sandbox.Invoke($"Import-Module {Literal(module)} -Force\n" +
            $"Import-Module {Literal(Path.Combine(FindRoot(), "scripts", "RimWorldEndToEndRunner.Support.psm1"))} -Force\n" +
            $"$receipt = {Literal(JsonSerializer.Serialize(receipt))} | ConvertFrom-Json\n" +
            "$launch = New-RimWorldEndToEndSmokeLaunch -Arguments @('-Quicktest','-RunEndToEndTests','-SkipBuildDeploy') -FixtureExpectations $receipt\n" +
            "$arguments = $launch.Arguments\n" +
            "$expectations = Read-RimWorldFixtureExpectations -Path $arguments[([Array]::IndexOf($arguments, '-XmlFixtureExpectationsFile') + 1)] -Sha256 $arguments[([Array]::IndexOf($arguments, '-XmlFixtureExpectationsSha256') + 1)] -ActivePackageIds @('ludeon.rimworld','fumblesneeze.owner','fumblesneeze.fixture','fumblesneeze.rimworlddevgateway')\n" +
            $"Get-RimWorldFixtureLogAccounting -Path {Literal(sandbox.Log)} -Expectations $expectations -Final:${(mode != "missing-startup").ToString().ToLowerInvariant()} | ConvertTo-Json -Depth 15\n");
        using var result = JsonDocument.Parse(output);
        Assert.That(result.RootElement.GetProperty("Passed").GetBoolean(), Is.EqualTo(passes), output);
        var expected = result.RootElement.GetProperty("Expected")[0];
        Assert.That(expected.GetProperty("ObservedCount").GetInt32(), Is.EqualTo(matchedCount), output);
        if (matchedCount > 0)
            Assert.That(expected.GetProperty("Matches")[0].GetProperty("Lines").GetRawText(), Does.Contain("Verse."));
        if (mode is "info-error-continuation" or "warning-error-continuation")
        {
            var lines = result.RootElement.GetProperty("Unexpected")[0].GetProperty("Lines");
            Assert.That(lines.EnumerateArray().Select(value => value.GetString()), Is.EqualTo(new[] {
                "ERROR " + Message, "    at Verse.Log.Error(String text)"
            }));
        }
    }

    [TestCase("tampered")]
    [TestCase("foreign-package")]
    [TestCase("duplicate-fields")]
    [TestCase("missing-gateway")]
    [TestCase("duplicate-gateway")]
    [TestCase("reordered")]
    public void Smoke_rejects_changed_or_foreign_expectation_file(string mode)
    {
        using var sandbox = new Sandbox();
        var receipt = sandbox.WriteReceipt();
        if (mode == "tampered") File.AppendAllText(receipt.Path, " ");
        if (mode == "duplicate-fields")
        {
            File.WriteAllText(receipt.Path, File.ReadAllText(receipt.Path).Replace("\"expectedErrors\":", "\"expectedErrors\":[],\"expectedErrors\":"));
            receipt = receipt with { Sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(receipt.Path))) };
        }
        string active = mode == "foreign-package" ? "'ludeon.rimworld','fumblesneeze.other','fumblesneeze.fixture','fumblesneeze.rimworlddevgateway'" : "'ludeon.rimworld','fumblesneeze.owner','fumblesneeze.fixture','fumblesneeze.rimworlddevgateway'";
        if (mode == "missing-gateway") active = "'ludeon.rimworld','fumblesneeze.owner','fumblesneeze.fixture'";
        if (mode == "duplicate-gateway") active += ",'fumblesneeze.rimworlddevgateway'";
        if (mode == "reordered") active = "'ludeon.rimworld','fumblesneeze.fixture','fumblesneeze.owner','fumblesneeze.rimworlddevgateway'";
        string output = sandbox.Invoke($"Import-Module {Literal(Path.Combine(FindRoot(), "scripts", "RimWorldFixtureDiagnostics.Support.psm1"))} -Force\n" +
            $"try {{ $null = Read-RimWorldFixtureExpectations -Path {Literal(receipt.Path)} -Sha256 '{receipt.Sha256}' -ActivePackageIds @({active}); throw 'unexpected acceptance' }} catch {{ if ($_.Exception.Message -eq 'unexpected acceptance') {{ throw }}; $_.Exception.Message }}\n");
        Assert.That(output, Does.Not.Contain("unexpected acceptance"));
    }

    [TestCase("expected", true)]
    [TestCase("unexpected", false)]
    [TestCase("duplicate", false)]
    public void Final_scan_rechecks_late_appended_diagnostics_and_retains_failure_evidence(string appended, bool passes)
    {
        using var sandbox = new Sandbox();
        var receipt = sandbox.WriteReceipt();
        File.WriteAllText(sandbox.Log, appended == "duplicate" ? "21:03:07 ERROR " + Message + "\n" : "21:03:07 INFO Starting\n");
        string late = appended == "unexpected" ? "21:03:08 ERROR Foreign error\n" : "21:03:08 ERROR " + Message + "\n21:03:08    at Verse.DirectXmlToObjectNew.DefFromNodeNew()\n";
        string evidence = Path.Combine(sandbox.Root, "final-accounting.json");
        string smoke = Path.Combine(FindRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
        string output = sandbox.Invoke($"Import-Module {Literal(Path.Combine(FindRoot(), "scripts", "RimWorldFixtureDiagnostics.Support.psm1"))} -Force\n" +
            $"$expectations = Read-RimWorldFixtureExpectations -Path {Literal(receipt.Path)} -Sha256 '{receipt.Sha256}' -ActivePackageIds @('ludeon.rimworld','fumblesneeze.owner','fumblesneeze.fixture','fumblesneeze.rimworlddevgateway')\n" +
            $"$initial = Get-RimWorldFixtureLogAccounting -Path {Literal(sandbox.Log)} -Expectations $expectations\n" +
            "if (-not $initial.Passed) { throw 'Initial scan unexpectedly failed' }\n" +
            $"[IO.File]::AppendAllText({Literal(sandbox.Log)}, {Literal(late)})\n" +
            "$tokens=$null; $errors=$null\n" +
            $"$ast = [Management.Automation.Language.Parser]::ParseFile({Literal(smoke)}, [ref]$tokens, [ref]$errors)\n" +
            "$function = $ast.Find({ param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq 'Assert-GatewayFinalPlayerLog' }, $true)\nInvoke-Expression $function.Extent.Text\n" +
            $"try {{ Assert-GatewayFinalPlayerLog -Path {Literal(sandbox.Log)} -BearerToken '' -FixtureExpectations $expectations -EvidencePath {Literal(evidence)} }} catch {{ }}\n" +
            $"Get-Content -LiteralPath {Literal(evidence)} -Raw\n");
        using var result = JsonDocument.Parse(output);
        Assert.That(result.RootElement.GetProperty("Passed").GetBoolean(), Is.EqualTo(passes), output);
        Assert.That(result.RootElement.GetProperty("Phase").GetString(), Is.EqualTo("final"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Selected_compiled_receipt_resolves_only_its_owned_save_path_and_accounts_native_warning(bool gatewayOwner)
    {
        using var sandbox = new Sandbox();
        var selected = sandbox.WriteSelectedReceipt(gatewayOwner);
        string active = gatewayOwner ? "'brrainz.harmony','ludeon.rimworld','fumblesneeze.rimworlddevgateway'" : "'brrainz.harmony','ludeon.rimworld','alpha.mod','optional.mod','fumblesneeze.rimworlddevgateway'";
        string message = SelectedTestDiagnosticTests.Rule().Message.Replace("{savedata}", selected.SavedData);
        string record = "21:03:07 WARN " + message + "\n" + SelectedTestDiagnosticTests.Rule().InnerCauses.Single() + "\n   at OwnedFixture.Fail()\n";
        File.WriteAllText(sandbox.Log, record + record);
        var output = sandbox.Invoke($"Import-Module {Literal(Path.Combine(FindRoot(), "scripts", "RimWorldFixtureDiagnostics.Support.psm1"))} -Force\n" +
            $"Import-Module {Literal(Path.Combine(FindRoot(), "scripts", "RimWorldEndToEndRunner.Support.psm1"))} -Force\n" +
            $"$receipt = {Literal(JsonSerializer.Serialize(selected.Receipt))} | ConvertFrom-Json\n" +
            "$launch = New-RimWorldEndToEndSmokeLaunch -Arguments @('-Quicktest','-RunEndToEndTests','-SkipBuildDeploy','-EndToEndTestIds','alpha.optional') -SelectedTestExpectations $receipt\n" +
            "$arguments = $launch.Arguments\n" +
            "$expectations = Read-RimWorldSelectedTestExpectations -Path $arguments[([Array]::IndexOf($arguments, '-SelectedTestExpectationsFile') + 1)] -Sha256 $arguments[([Array]::IndexOf($arguments, '-SelectedTestExpectationsSha256') + 1)] " +
            $"-ActivePackageIds @({active}) -SelectedTestIds @('alpha.optional') " +
            $"-ModsRoot {Literal(selected.ModsRoot)} -RimWorldVersion '1.7' -SavedDataPath {Literal(selected.SavedData)}\n" +
            $"Get-RimWorldFixtureLogAccounting -Path {Literal(sandbox.Log)} -Expectations $expectations -Final | ConvertTo-Json -Depth 16\n");
        using var result = JsonDocument.Parse(output);
        Assert.That(result.RootElement.GetProperty("Passed").GetBoolean(), Is.True, output);
        Assert.That(result.RootElement.GetProperty("Expected")[0].GetProperty("ObservedCount").GetInt32(), Is.EqualTo(2));
        Assert.That(result.RootElement.GetProperty("Expected")[0].GetProperty("TestId").GetString(), Is.EqualTo("alpha.optional"));
        Assert.That(result.RootElement.GetProperty("Expected")[0].GetProperty("OwnerPackageId").GetString(), Is.EqualTo(gatewayOwner ? "fumblesneeze.rimworlddevgateway" : "alpha.mod"));
    }

    [TestCase("wrong-test")]
    [TestCase("multiple-tests")]
    [TestCase("wrong-group")]
    [TestCase("changed-assembly")]
    [TestCase("changed-manifest")]
    [TestCase("changed-receipt")]
    [TestCase("unknown-field")]
    [TestCase("duplicate-field")]
    [TestCase("severity")]
    [TestCase("count")]
    [TestCase("declaration-drift")]
    [TestCase("other-token")]
    [TestCase("traversal")]
    [TestCase("wildcard")]
    [TestCase("wrong-root")]
    public void Selected_receipt_rejects_foreign_selection_or_modified_provenance(string mode)
    {
        using var sandbox = new Sandbox();
        var selected = sandbox.WriteSelectedReceipt();
        var receipt = selected.Receipt;
        var document = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(receipt.Path))!;
        if (mode == "changed-assembly") File.AppendAllText(Directory.GetFiles(selected.ModsRoot, "*.dll", SearchOption.AllDirectories).Single(), "changed");
        if (mode == "changed-manifest") File.AppendAllText(Directory.GetFiles(selected.ModsRoot, "*.json", SearchOption.AllDirectories).Single(), "changed");
        if (mode == "unknown-field") document["ignoreEverything"] = true;
        var rule = document["expectedDiagnostics"]![0]!;
        if (mode == "severity") rule["severity"] = "FATAL";
        if (mode == "count") rule["count"] = 0;
        if (mode == "declaration-drift") rule["count"] = 1;
        if (mode == "other-token") rule["message"] = "Fault {other}";
        if (mode == "traversal") rule["message"] = "Fault at \"{savedata}\\Saves\\..\\foreign.rws\"";
        if (mode == "wildcard") rule["message"] = "Fault at \"{savedata}\\Saves\\*.rws\"";
        File.WriteAllText(receipt.Path, document.ToJsonString());
        if (mode == "duplicate-field") File.WriteAllText(receipt.Path, File.ReadAllText(receipt.Path).Replace("\"testId\":", "\"testId\":\"foreign\",\"testId\":"));
        receipt = receipt with { Sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(receipt.Path))) };
        if (mode == "changed-receipt") File.AppendAllText(receipt.Path, " ");
        string testIds = mode == "wrong-test" ? "'alpha.base-a'" : mode == "multiple-tests" ? "'alpha.optional','alpha.base-a'" : "'alpha.optional'";
        string active = mode == "wrong-group" ? "'ludeon.rimworld','brrainz.harmony','alpha.mod','optional.mod','fumblesneeze.rimworlddevgateway'" : "'brrainz.harmony','ludeon.rimworld','alpha.mod','optional.mod','fumblesneeze.rimworlddevgateway'";
        string output = sandbox.Invoke($"Import-Module {Literal(Path.Combine(FindRoot(), "scripts", "RimWorldFixtureDiagnostics.Support.psm1"))} -Force\n" +
            $"try {{ $null = Read-RimWorldSelectedTestExpectations -Path {Literal(receipt.Path)} -Sha256 '{receipt.Sha256}' -ActivePackageIds @({active}) -SelectedTestIds @({testIds}) " +
            $"-ModsRoot {Literal(selected.ModsRoot)} -RimWorldVersion '1.7' -SavedDataPath {Literal(mode == "wrong-root" ? sandbox.Root : selected.SavedData)}; throw 'unexpected acceptance' }} catch {{ if ($_.Exception.Message -eq 'unexpected acceptance') {{ throw }}; $_.Exception.Message }}\n");
        Assert.That(output, Does.Not.Contain("unexpected acceptance"));
    }

    [TestCase("wrong-file")]
    [TestCase("wrong-run")]
    [TestCase("extra-inner-line")]
    [TestCase("timestamped-inner-line")]
    public void Selected_receipt_does_not_allow_nearby_paths_or_undeclared_inner_lines(string mode)
    {
        using var sandbox = new Sandbox();
        var selected = sandbox.WriteSelectedReceipt();
        var rule = SelectedTestDiagnosticTests.Rule();
        string message = rule.Message.Replace("{savedata}", selected.SavedData);
        if (mode == "wrong-file") message = message.Replace("Owned-Fault", "Unrelated-Fault");
        if (mode == "wrong-run") message = message.Replace(selected.SavedData, Path.Combine(sandbox.Root, "other-run", "SavedData"));
        string cause = (mode == "timestamped-inner-line" ? "21:03:07 " : "") + rule.InnerCauses.Single();
        string record = "21:03:07 WARN " + message + "\n" + cause + "\n" + (mode == "extra-inner-line" ? " ---> Owned.Failure: other cause\n" : "");
        File.WriteAllText(sandbox.Log, record + record);
        string output = sandbox.Invoke($"Import-Module {Literal(Path.Combine(FindRoot(), "scripts", "RimWorldFixtureDiagnostics.Support.psm1"))} -Force\n" +
            $"$expectations = Read-RimWorldSelectedTestExpectations -Path {Literal(selected.Receipt.Path)} -Sha256 '{selected.Receipt.Sha256}' " +
            $"-ActivePackageIds @('brrainz.harmony','ludeon.rimworld','alpha.mod','optional.mod','fumblesneeze.rimworlddevgateway') -SelectedTestIds @('alpha.optional') " +
            $"-ModsRoot {Literal(selected.ModsRoot)} -RimWorldVersion '1.7' -SavedDataPath {Literal(selected.SavedData)}\n" +
            $"Get-RimWorldFixtureLogAccounting -Path {Literal(sandbox.Log)} -Expectations $expectations -Final | ConvertTo-Json -Depth 16\n");
        using var result = JsonDocument.Parse(output);
        Assert.That(result.RootElement.GetProperty("Passed").GetBoolean(), Is.False, output);
    }

    private static string Literal(string value) => "'" + value.Replace("'", "''") + "'";
    private static string FindRoot()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "openspec"))) root = root.Parent;
        return root?.FullName ?? throw new DirectoryNotFoundException();
    }

    private sealed class Sandbox : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "FixtureDiagnosticTests-" + Guid.NewGuid().ToString("N"));
        public string Log => Path.Combine(Root, "Player.log");
        public Sandbox() => Directory.CreateDirectory(Root);
        public EndToEndXmlFixtureExpectationReceipt WriteReceipt(bool includeHarmony = false)
        {
            string source = Path.Combine(Root, "tests", "Fixtures", "Diagnostic");
            Directory.CreateDirectory(Path.Combine(source, "About"));
            Directory.CreateDirectory(Path.Combine(source, "Defs"));
            File.WriteAllText(Path.Combine(source, "About", "About.xml"), "<ModMetaData><packageId>fumblesneeze.fixture</packageId><supportedVersions><li>1.7</li></supportedVersions><modDependencies><li><packageId>fumblesneeze.owner</packageId></li></modDependencies></ModMetaData>");
            File.WriteAllText(Path.Combine(source, "Defs", "Diagnostic.xml"), "<Defs><DeliberatelyMissingDefType/></Defs>");
            string manifest = Path.Combine(source, "fixture.json");
            File.WriteAllText(manifest, JsonSerializer.Serialize(new { schema = "RimWorldDevGateway/XmlFixture/v1", ownerPackageId = "fumblesneeze.owner", packageId = "fumblesneeze.fixture", rimWorldVersion = "1.7", packageInclude = new[] { "About/About.xml", "Defs/Diagnostic.xml" }, expectedErrors = new[] { new { message = Message, count = 1 } } }));
            var active = new[] { "ludeon.rimworld", "fumblesneeze.owner", "fumblesneeze.fixture" };
            if (includeHarmony) active = new[] { "brrainz.harmony" }.Concat(active).ToArray();
            var plans = EndToEndXmlFixturePlanner.PlanSelected(Root, new[] { manifest }, active, Array.Empty<string>(), Path.Combine(Root, "Mods"), "1.7");
            return EndToEndXmlFixtureExpectations.Write(plans, Path.Combine(Root, "fixture.lease.json"));
        }
        public (EndToEndXmlFixtureExpectationReceipt Receipt, string ModsRoot, string SavedData) WriteSelectedReceipt(bool gatewayOwner = false)
        {
            var candidate = SelectedTestDiagnosticTests.Candidate(new[] { SelectedTestDiagnosticTests.Rule() }, gatewayOwner);
            string mods = Path.Combine(Root, "Mods");
            var plan = EndToEndBundlePlanner.Create(new[] { candidate }, candidate.Metadata.Declarations.Single().ActivePackageIds,
                mods, "1.7", new[] { "alpha.optional" }, Array.Empty<string>());
            var owner = plan.OwnerStages.Single();
            var bundle = owner.Bundles.Single();
            Directory.CreateDirectory(owner.DestinationDirectory);
            File.Copy(bundle.SourceAssemblyPath, Path.Combine(owner.DestinationDirectory, bundle.AssemblyFileName));
            File.WriteAllText(Path.Combine(owner.DestinationDirectory, bundle.ManifestFileName), bundle.ManifestJson, new UTF8Encoding(false));
            string savedData = Path.Combine(Root, "run", "SavedData");
            Directory.CreateDirectory(savedData);
            return (EndToEndSelectedTestExpectations.Write(plan, Path.Combine(Root, "stage.lease.json"))!, mods, savedData);
        }
        public string Invoke(string body)
        {
            string script = Path.Combine(Root, "invoke.ps1");
            File.WriteAllText(script, "$ErrorActionPreference='Stop'\nSet-StrictMode -Version Latest\n" + body, new UTF8Encoding(false));
            var start = new ProcessStartInfo("pwsh.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string arg in new[] { "-NoProfile", "-NonInteractive", "-File", script }) start.ArgumentList.Add(arg);
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(entireProcessTree: true); throw new TimeoutException(); }
            Assert.That(process.ExitCode, Is.Zero, stderr.GetAwaiter().GetResult());
            return stdout.GetAwaiter().GetResult();
        }
        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
