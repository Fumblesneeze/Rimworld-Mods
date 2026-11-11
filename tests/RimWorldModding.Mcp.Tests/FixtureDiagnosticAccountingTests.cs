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
    public void Expectation_receipt_is_create_new_and_cannot_overwrite_an_existing_handoff()
    {
        using var sandbox = new Sandbox();
        var receipt = sandbox.WriteReceipt();
        string original = File.ReadAllText(receipt.Path);
        Assert.Throws<IOException>(() => sandbox.WriteReceipt());
        Assert.That(File.ReadAllText(receipt.Path), Is.EqualTo(original));
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
