using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayLocaleProviderContractTests
{
    private static readonly string[] RequiredNonEnglishLocales =
    {
        "German",
        "Spanish",
        "French",
        "ChineseSimplified",
        "Russian",
        "Japanese"
    };

    [Test]
    public void Development_package_provides_metadata_only_locale_activation_for_product_e2e_runs()
    {
        var root = FindRepositoryRoot();
        var languageRoot = Path.Combine(root, "mods", "RimWorldDevGateway", "Languages");
        var packagedLanguageRoot = Path.Combine(
            root,
            "artifacts",
            "Mods",
            "fumblesneeze.rimworlddevgateway",
            "Languages");
        var actual = Directory.Exists(languageRoot)
            ? Directory.GetDirectories(languageRoot).Select(Path.GetFileName).OrderBy(value => value, StringComparer.Ordinal).ToArray()
            : Array.Empty<string>();

        Assert.That(actual, Is.EqualTo(RequiredNonEnglishLocales.OrderBy(value => value, StringComparer.Ordinal)));
        foreach (var locale in RequiredNonEnglishLocales)
        {
            var path = Path.Combine(languageRoot, locale, "LanguageInfo.xml");
            var packagedPath = Path.Combine(packagedLanguageRoot, locale, "LanguageInfo.xml");
            var obsoleteVersionedPath = Path.Combine(
                root,
                "artifacts",
                "Mods",
                "fumblesneeze.rimworlddevgateway",
                "1.6",
                "Languages",
                locale,
                "LanguageInfo.xml");
            Assert.That(File.Exists(path), Is.True, $"Missing metadata-only locale provider for {locale}.");
            Assert.That(File.Exists(packagedPath), Is.True,
                $"The locale provider for {locale} must be at package root so RimWorld can discover it before resolving versioned load folders.");
            Assert.That(File.ReadAllBytes(packagedPath), Is.EqualTo(File.ReadAllBytes(path)));
            Assert.That(File.Exists(obsoleteVersionedPath), Is.False,
                "LanguageInfo must not be duplicated in a versioned folder that RimWorld discovers too late.");
            var document = XDocument.Load(path);
            Assert.Multiple(() =>
            {
                Assert.That(document.Root?.Name.LocalName, Is.EqualTo("LanguageInfo"));
                Assert.That(document.Root?.Element("friendlyNameNative")?.Value, Is.Not.Empty);
                Assert.That(document.Root?.Element("friendlyNameEnglish")?.Value, Is.EqualTo(locale));
                Assert.That(document.Root?.Element("canBeTiny")?.Value, Is.EqualTo("true"));
                Assert.That(document.Descendants("Keyed").Any(), Is.False,
                    "The Gateway must activate a locale for isolated verification without pretending to translate RimWorld or a product mod.");
            });
        }
    }

    [Test]
    public void Launcher_publishes_locale_metadata_with_exclusive_creation_and_failure_cleanup()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "Invoke-GatewaySmoke.ps1"));
        var publishStart = script.IndexOf(
            "function Publish-GatewaySmokeLanguageProvider",
            StringComparison.Ordinal);
        var removeStart = script.IndexOf(
            "function Remove-GatewaySmokeLanguageProvider",
            StringComparison.Ordinal);

        Assert.That(publishStart, Is.GreaterThanOrEqualTo(0));
        Assert.That(removeStart, Is.GreaterThan(publishStart));
        var publish = script.Substring(publishStart, removeStart - publishStart);
        Assert.Multiple(() =>
        {
            Assert.That(publish, Does.Contain("[System.IO.FileMode]::CreateNew"),
                "A provider appearing after planning must fail closed rather than be overwritten by Copy-Item.");
            Assert.That(publish, Does.Not.Contain("Copy-Item"));
            Assert.That(publish, Does.Contain("Remove-Item -LiteralPath $targetPath -Force"),
                "A failed copy after exclusive creation must remove its exact partial file.");
            Assert.That(publish, Does.Contain("@(Get-ChildItem -LiteralPath $targetDirectory -Force).Count -eq 0"),
                "A publish failure must remove only an empty directory created by this lease.");
            Assert.That(publish, Does.Contain("$sourceSha256 -cne $targetSha256"),
                "The publisher must reject a staged file that is not byte-identical to its packaged source.");
        });
    }

    [Test]
    public void Launcher_rejects_a_provider_that_appears_after_an_absent_plan_without_overwriting_it()
    {
        var root = FindRepositoryRoot();
        var rimWorldRoot = Path.Combine(Path.GetTempPath(), nameof(GatewayLocaleProviderContractTests), Guid.NewGuid().ToString("N"));
        var sourcePath = Path.Combine(rimWorldRoot, "Mods", "fumblesneeze.rimworlddevgateway", "Languages", "German", "LanguageInfo.xml");
        var targetDirectory = Path.Combine(rimWorldRoot, "Data", "Core", "Languages", "German");
        var targetPath = Path.Combine(targetDirectory, "LanguageInfo.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
        Directory.CreateDirectory(targetDirectory);
        File.WriteAllText(sourcePath, "packaged");
        File.WriteAllText(targetPath, "borrowed-by-another-run");
        try
        {
            var result = RunPowerShell(
                ExtractFunctionScript(root, "Publish-GatewaySmokeLanguageProvider") +
                "$plan = [pscustomobject]@{ Language='German'; Mode='planned-staged-metadata'; " +
                $"TargetDirectory='{EscapePowerShell(targetDirectory)}'; TargetPath='{EscapePowerShell(targetPath)}' }}\n" +
                $"try {{ Publish-GatewaySmokeLanguageProvider -Plan $plan -RimWorldPath '{EscapePowerShell(rimWorldRoot)}' | Out-Null; exit 80 }} catch {{ }}\n" +
                $"if ((Get-Content -LiteralPath '{EscapePowerShell(targetPath)}' -Raw) -cne 'borrowed-by-another-run') {{ exit 81 }}\n");

            Assert.That(result.ExitCode, Is.EqualTo(0), result.StandardError + result.StandardOutput);
        }
        finally
        {
            Directory.Delete(rimWorldRoot, recursive: true);
        }
    }

    [Test]
    public void Launcher_rolls_back_its_exact_file_and_empty_directory_when_post_copy_hashing_fails()
    {
        var root = FindRepositoryRoot();
        var rimWorldRoot = Path.Combine(Path.GetTempPath(), nameof(GatewayLocaleProviderContractTests), Guid.NewGuid().ToString("N"));
        var sourcePath = Path.Combine(rimWorldRoot, "Mods", "fumblesneeze.rimworlddevgateway", "Languages", "German", "LanguageInfo.xml");
        var targetDirectory = Path.Combine(rimWorldRoot, "Data", "Core", "Languages", "German");
        var targetPath = Path.Combine(targetDirectory, "LanguageInfo.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
        File.WriteAllText(sourcePath, "packaged");
        try
        {
            var result = RunPowerShell(
                ExtractFunctionScript(root, "Publish-GatewaySmokeLanguageProvider") +
                "function Get-GatewaySmokeStreamSha256 { throw 'post-copy hash probe failed' }\n" +
                "$plan = [pscustomobject]@{ Language='German'; Mode='planned-staged-metadata'; " +
                $"TargetDirectory='{EscapePowerShell(targetDirectory)}'; TargetPath='{EscapePowerShell(targetPath)}' }}\n" +
                $"try {{ Publish-GatewaySmokeLanguageProvider -Plan $plan -RimWorldPath '{EscapePowerShell(rimWorldRoot)}' | Out-Null; exit 82 }} catch {{ }}\n" +
                $"if (Test-Path -LiteralPath '{EscapePowerShell(targetPath)}') {{ exit 83 }}\n" +
                $"if (Test-Path -LiteralPath '{EscapePowerShell(targetDirectory)}') {{ exit 84 }}\n");

            Assert.That(result.ExitCode, Is.EqualTo(0), result.StandardError + result.StandardOutput);
        }
        finally
        {
            if (Directory.Exists(rimWorldRoot))
            {
                Directory.Delete(rimWorldRoot, recursive: true);
            }
        }
    }

    [Test]
    public void Launcher_holds_one_cross_process_locale_lease_from_replanning_through_cleanup()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1"));
        var acquire = script.IndexOf(
            "$languageProviderLease = Enter-GatewaySmokeLanguageProviderLease",
            StringComparison.Ordinal);
        var replan = acquire < 0 ? -1 : script.IndexOf(
            "$languageProviderPlan = Get-GatewaySmokeLanguageProviderPlan",
            acquire,
            StringComparison.Ordinal);
        var cleanup = replan < 0 ? -1 : script.IndexOf(
            "$languageProviderCleanup = Remove-GatewaySmokeLanguageProvider",
            replan,
            StringComparison.Ordinal);
        var release = cleanup < 0 ? -1 : script.IndexOf(
            "Exit-GatewaySmokeLanguageProviderLease -Lease $languageProviderLease",
            cleanup,
            StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("$mutex.WaitOne(30000)"),
                "Concurrent launchers must wait only a bounded time for the exact destination lease.");
            Assert.That(acquire, Is.GreaterThanOrEqualTo(0));
            Assert.That(replan, Is.GreaterThan(acquire),
                "Provider presence must be planned again only after the cross-process lease is owned.");
            Assert.That(cleanup, Is.GreaterThan(replan));
            Assert.That(release, Is.GreaterThan(cleanup),
                "The lease must remain owned until after exact provider cleanup.");
        });
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "RimWorldMods.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private static string ExtractFunctionScript(string repositoryRoot, string functionName)
    {
        var scriptPath = Path.Combine(repositoryRoot, "scripts", "Invoke-GatewaySmoke.ps1");
        return
            $"$tokens=$null; $errors=$null; $ast=[System.Management.Automation.Language.Parser]::ParseFile('{EscapePowerShell(scriptPath)}',[ref]$tokens,[ref]$errors)\n" +
            $"$functionAst=$ast.Find({{ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq '{functionName}' }},$true)\n" +
            "if ($null -eq $functionAst) { throw 'Function not found.' }\n" +
            "Invoke-Expression $functionAst.Extent.Text\n";
    }

    private static PowerShellResult RunPowerShell(string script)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                Arguments = "-NoProfile -NonInteractive -Command -",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        process.StandardInput.Write(script);
        process.StandardInput.Close();
        if (!process.WaitForExit(10_000))
        {
            try
            {
                process.Kill();
            }
            finally
            {
                process.WaitForExit(5_000);
            }
            throw new AssertionException("The owned PowerShell locale-provider probe exceeded ten seconds.");
        }
        if (!System.Threading.Tasks.Task.WaitAll(
                new System.Threading.Tasks.Task[] { standardOutputTask, standardErrorTask },
                5_000))
        {
            throw new AssertionException("The exited PowerShell locale-provider probe did not drain output.");
        }
        var standardOutput = standardOutputTask.GetAwaiter().GetResult();
        var standardError = standardErrorTask.GetAwaiter().GetResult();
        return new PowerShellResult(process.ExitCode, standardOutput, standardError);
    }

    private static string EscapePowerShell(string value) => value.Replace("'", "''");

    private sealed class PowerShellResult
    {
        internal PowerShellResult(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }

        internal int ExitCode { get; }
        internal string StandardOutput { get; }
        internal string StandardError { get; }
    }
}
