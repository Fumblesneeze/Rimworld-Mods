using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
[NonParallelizable]
public sealed class GatewaySmokeProductModEvidenceTests
{
    [Test]
    public void Additional_mod_project_paths_are_resolved_under_Windows_PowerShell_5_1()
    {
        var root = Path.Combine(Path.GetTempPath(), "GatewaySmokePs51RelativePath", Guid.NewGuid().ToString("N"));
        var projectDirectory = Path.Combine(root, "mods", "Product");
        var projectPath = Path.Combine(projectDirectory, "Product.csproj");
        Directory.CreateDirectory(projectDirectory);
        try
        {
            File.WriteAllText(
                projectPath,
                "<Project><PropertyGroup><RimWorldPackageId>product.mod</RimWorldPackageId>" +
                "<AssemblyName>Product</AssemblyName></PropertyGroup></Project>",
                new UTF8Encoding(false));

            var result = InvokeWindowsPowerShell(
                root,
                "$project = Get-GatewaySmokeAdditionalModProject -RepositoryRoot $fixtureRoot -ProjectPath $projectPath -AdditionalPackageIds @('product.mod')\n" +
                "$project.RelativeProjectPath");

            Assert.Multiple(() =>
            {
                Assert.That(result.ExitCode, Is.Zero, result.StandardError);
                Assert.That(result.StandardOutput.Trim(), Is.EqualTo("mods/Product/Product.csproj"));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Ordinary_product_package_rejects_a_metadata_bearing_dll_without_an_assembly_definition()
    {
        var root = Path.Combine(Path.GetTempPath(), "GatewaySmokeProductModEvidenceTests", Guid.NewGuid().ToString("N"));
        var projectDirectory = Path.Combine(root, "mods", "Product");
        var projectPath = Path.Combine(projectDirectory, "Product.csproj");
        var generationDirectory = Path.Combine(root, "managed-module-fixture");
        var game = Path.Combine(root, "game");
        var deployed = Path.Combine(game, "Mods", "product.mod");
        var deployedAssemblies = Path.Combine(deployed, "1.6", "Assemblies");
        var deployedAbout = Path.Combine(deployed, "About");
        Directory.CreateDirectory(projectDirectory);
        Directory.CreateDirectory(generationDirectory);
        Directory.CreateDirectory(deployedAssemblies);
        Directory.CreateDirectory(deployedAbout);
        try
        {
            const string netModuleFileName = "ManagedNetModule.dll";
            var moduleAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("ManagedNetModuleManifest"),
                AssemblyBuilderAccess.Save,
                generationDirectory);
            var moduleBuilder = moduleAssembly.DefineDynamicModule("ManagedNetModule", netModuleFileName);
            var typeBuilder = moduleBuilder.DefineType(
                "Product.NetModuleGatewayReferenceProbe",
                TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
            typeBuilder.DefineField(
                "GatewayPhase",
                typeof(RimWorldDevGateway.DispatchPhase),
                FieldAttributes.Public | FieldAttributes.Static);
            typeBuilder.CreateType();
            moduleAssembly.Save("ManagedNetModuleManifest.dll");

            var cleanAssembly = typeof(TestAttribute).Assembly;
            var assemblyName = cleanAssembly.GetName().Name!;
            File.WriteAllText(
                projectPath,
                "<Project><PropertyGroup><RimWorldPackageId>product.mod</RimWorldPackageId>" +
                "<AssemblyName>" + assemblyName + "</AssemblyName></PropertyGroup></Project>",
                new UTF8Encoding(false));
            File.Copy(cleanAssembly.Location, Path.Combine(deployedAssemblies, assemblyName + ".dll"));
            File.Copy(
                Path.Combine(generationDirectory, netModuleFileName),
                Path.Combine(deployedAssemblies, netModuleFileName));
            File.WriteAllText(Path.Combine(deployedAbout, "About.xml"), "<ModMetaData />", new UTF8Encoding(false));

            var rejected = Invoke(
                root,
                "$project = Get-GatewaySmokeAdditionalModProject -RepositoryRoot $fixtureRoot -ProjectPath $projectPath -AdditionalPackageIds @('product.mod')\n" +
                "Get-GatewaySmokeProductPackageEvidence -Project $project -RimWorldPath $gamePath -BuildLogPath 'build.log' | Out-Null");

            Assert.Multiple(() =>
            {
                Assert.That(rejected.ExitCode, Is.EqualTo(1));
                Assert.That(rejected.StandardError, Does.Contain("ManagedNetModule.dll"));
                Assert.That(rejected.StandardError, Does.Contain("assembly definition").IgnoreCase);
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Product_package_evidence_retains_every_file_and_metadata_only_managed_references_deterministically()
    {
        var root = Path.Combine(Path.GetTempPath(), "GatewaySmokeProductModEvidenceTests", Guid.NewGuid().ToString("N"));
        var projectDirectory = Path.Combine(root, "mods", "Product");
        var projectPath = Path.Combine(projectDirectory, "Product.csproj");
        var sourcePatches = Path.Combine(projectDirectory, "Patches");
        var game = Path.Combine(root, "game");
        var deployed = Path.Combine(game, "Mods", "product.mod");
        var deployedAssemblies = Path.Combine(deployed, "1.6", "Assemblies");
        var deployedPatches = Path.Combine(deployed, "1.6", "Patches");
        var deployedAbout = Path.Combine(deployed, "About");
        var deployedTextures = Path.Combine(deployed, "Textures");
        Directory.CreateDirectory(sourcePatches);
        Directory.CreateDirectory(deployedAssemblies);
        Directory.CreateDirectory(deployedPatches);
        Directory.CreateDirectory(deployedAbout);
        Directory.CreateDirectory(deployedTextures);
        try
        {
            var cleanAssembly = typeof(TestAttribute).Assembly;
            var secondManagedAssembly = typeof(Enumerable).Assembly;
            var assemblyName = cleanAssembly.GetName().Name!;
            var assemblyFileName = assemblyName + ".dll";
            var secondAssemblyFileName = secondManagedAssembly.GetName().Name + ".dll";
            File.WriteAllText(
                projectPath,
                "<Project><PropertyGroup><RimWorldPackageId>product.mod</RimWorldPackageId>" +
                "<AssemblyName>" + assemblyName + "</AssemblyName></PropertyGroup></Project>",
                new UTF8Encoding(false));
            File.Copy(cleanAssembly.Location, Path.Combine(deployedAssemblies, assemblyFileName));
            File.Copy(secondManagedAssembly.Location, Path.Combine(deployedAssemblies, secondAssemblyFileName));
            File.WriteAllText(Path.Combine(sourcePatches, "Probe.xml"), "<Patch><marker>source</marker></Patch>", new UTF8Encoding(false));
            File.Copy(Path.Combine(sourcePatches, "Probe.xml"), Path.Combine(deployedPatches, "Probe.xml"));
            File.WriteAllText(Path.Combine(deployedAbout, "About.xml"), "<ModMetaData />", new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(deployedTextures, "Plate.txt"), "asset", new UTF8Encoding(false));

            var accepted = Invoke(
                root,
                "$project = Get-GatewaySmokeAdditionalModProject -RepositoryRoot $fixtureRoot -ProjectPath $projectPath -AdditionalPackageIds @('product.mod')\n" +
                "$evidence = Get-GatewaySmokeProductPackageEvidence -Project $project -RimWorldPath $gamePath -BuildLogPath 'build.log'\n" +
                "$evidence.Files | ForEach-Object { 'FILE|' + $_.RelativePath + '|' + $_.Length + '|' + $_.Sha256 }\n" +
                "$evidence.ManagedAssemblies | ForEach-Object { $managedAssembly = $_; 'ASSEMBLY|' + $managedAssembly.RelativePath + '|' + $managedAssembly.AssemblyIdentity; $managedAssembly.References | ForEach-Object { 'REFERENCE|' + $managedAssembly.RelativePath + '|' + $_.AssemblyIdentity } }");

            var expectedLines = new List<string>();
            var files = new[]
            {
                new { RelativePath = "1.6/Assemblies/" + assemblyFileName, Path = Path.Combine(deployedAssemblies, assemblyFileName) },
                new { RelativePath = "1.6/Assemblies/" + secondAssemblyFileName, Path = Path.Combine(deployedAssemblies, secondAssemblyFileName) },
                new { RelativePath = "1.6/Patches/Probe.xml", Path = Path.Combine(deployedPatches, "Probe.xml") },
                new { RelativePath = "About/About.xml", Path = Path.Combine(deployedAbout, "About.xml") },
                new { RelativePath = "Textures/Plate.txt", Path = Path.Combine(deployedTextures, "Plate.txt") }
            };
            expectedLines.AddRange(files
                .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
                .Select(file => $"FILE|{file.RelativePath}|{new FileInfo(file.Path).Length}|{Sha256(file.Path)}"));
            var managedAssemblies = new[]
            {
                new { RelativePath = "1.6/Assemblies/" + assemblyFileName, Assembly = cleanAssembly },
                new { RelativePath = "1.6/Assemblies/" + secondAssemblyFileName, Assembly = secondManagedAssembly }
            };
            foreach (var managedAssembly in managedAssemblies.OrderBy(
                         item => item.RelativePath,
                         StringComparer.Ordinal))
            {
                expectedLines.Add(
                    $"ASSEMBLY|{managedAssembly.RelativePath}|{managedAssembly.Assembly.GetName().FullName}");
                expectedLines.AddRange(managedAssembly.Assembly
                    .GetReferencedAssemblies()
                    .OrderBy(reference => reference.Name, StringComparer.Ordinal)
                    .ThenBy(reference => reference.FullName, StringComparer.Ordinal)
                    .Select(reference =>
                        $"REFERENCE|{managedAssembly.RelativePath}|{reference.FullName}"));
            }
            var actualLines = accepted.StandardOutput.Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.RemoveEmptyEntries);

            Assert.Multiple(() =>
            {
                Assert.That(accepted.ExitCode, Is.Zero, accepted.StandardError);
                Assert.That(actualLines, Is.EqualTo(expectedLines));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Ordinary_product_package_rejects_a_managed_reference_to_any_gateway_assembly()
    {
        var root = Path.Combine(Path.GetTempPath(), "GatewaySmokeProductModEvidenceTests", Guid.NewGuid().ToString("N"));
        var projectDirectory = Path.Combine(root, "mods", "Product");
        var projectPath = Path.Combine(projectDirectory, "Product.csproj");
        var referenceFixtureDirectory = Path.Combine(root, "reference-fixture");
        var game = Path.Combine(root, "game");
        var deployed = Path.Combine(game, "Mods", "product.mod");
        var deployedAssemblies = Path.Combine(deployed, "1.6", "Assemblies");
        var deployedAbout = Path.Combine(deployed, "About");
        Directory.CreateDirectory(projectDirectory);
        Directory.CreateDirectory(referenceFixtureDirectory);
        Directory.CreateDirectory(deployedAssemblies);
        Directory.CreateDirectory(deployedAbout);
        try
        {
            const string assemblyName = "ProductWithGatewayReference";
            var mixedCaseGatewayAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("rImWoRlDdEvGaTeWaY.Contracts"),
                AssemblyBuilderAccess.Save,
                referenceFixtureDirectory);
            var mixedCaseGatewayModule = mixedCaseGatewayAssembly.DefineDynamicModule(
                "MixedCaseGatewayContract",
                "rImWoRlDdEvGaTeWaY.Contracts.dll");
            var mixedCaseGatewayType = mixedCaseGatewayModule.DefineType(
                    "Gateway.HiddenContract",
                    TypeAttributes.Public | TypeAttributes.Class)
                .CreateType();
            mixedCaseGatewayAssembly.Save("rImWoRlDdEvGaTeWaY.Contracts.dll");
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(assemblyName),
                AssemblyBuilderAccess.Save,
                deployedAssemblies);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName, assemblyName + ".dll");
            var typeBuilder = moduleBuilder.DefineType(
                "Product.GatewayReferenceProbe",
                TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
            typeBuilder.DefineField(
                "GatewayContract",
                mixedCaseGatewayType,
                FieldAttributes.Public | FieldAttributes.Static);
            typeBuilder.CreateType();
            assemblyBuilder.Save(assemblyName + ".dll");
            File.WriteAllText(
                projectPath,
                "<Project><PropertyGroup><RimWorldPackageId>product.mod</RimWorldPackageId>" +
                "<AssemblyName>" + assemblyName + "</AssemblyName></PropertyGroup></Project>",
                new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(deployedAbout, "About.xml"), "<ModMetaData />", new UTF8Encoding(false));

            var rejected = Invoke(
                root,
                "$project = Get-GatewaySmokeAdditionalModProject -RepositoryRoot $fixtureRoot -ProjectPath $projectPath -AdditionalPackageIds @('product.mod')\n" +
                "Get-GatewaySmokeProductPackageEvidence -Project $project -RimWorldPath $gamePath -BuildLogPath 'build.log' | Out-Null");

            Assert.Multiple(() =>
            {
                Assert.That(rejected.ExitCode, Is.EqualTo(1));
                Assert.That(rejected.StandardError, Does.Contain("references").IgnoreCase);
                Assert.That(rejected.StandardError, Does.Contain("RimWorldDevGateway").IgnoreCase);
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Ordinary_product_package_rejects_every_gateway_named_dll_case_insensitively()
    {
        var root = Path.Combine(Path.GetTempPath(), "GatewaySmokeProductModEvidenceTests", Guid.NewGuid().ToString("N"));
        var projectDirectory = Path.Combine(root, "mods", "Product");
        var projectPath = Path.Combine(projectDirectory, "Product.csproj");
        var game = Path.Combine(root, "game");
        var deployed = Path.Combine(game, "Mods", "product.mod");
        var deployedAssemblies = Path.Combine(deployed, "1.6", "Assemblies");
        var deployedAbout = Path.Combine(deployed, "About");
        Directory.CreateDirectory(projectDirectory);
        Directory.CreateDirectory(deployedAssemblies);
        Directory.CreateDirectory(deployedAbout);
        try
        {
            var cleanAssembly = typeof(TestAttribute).Assembly;
            var assemblyName = cleanAssembly.GetName().Name!;
            File.WriteAllText(
                projectPath,
                "<Project><PropertyGroup><RimWorldPackageId>product.mod</RimWorldPackageId>" +
                "<AssemblyName>" + assemblyName + "</AssemblyName></PropertyGroup></Project>",
                new UTF8Encoding(false));
            File.Copy(cleanAssembly.Location, Path.Combine(deployedAssemblies, assemblyName + ".dll"));
            File.WriteAllText(Path.Combine(deployedAbout, "About.xml"), "<ModMetaData />", new UTF8Encoding(false));
            File.WriteAllText(
                Path.Combine(deployedAbout, "rImWoRlDdEvGaTeWaY.Contracts.DLL"),
                "forbidden",
                new UTF8Encoding(false));

            var rejected = Invoke(
                root,
                "$project = Get-GatewaySmokeAdditionalModProject -RepositoryRoot $fixtureRoot -ProjectPath $projectPath -AdditionalPackageIds @('product.mod')\n" +
                "Get-GatewaySmokeProductPackageEvidence -Project $project -RimWorldPath $gamePath -BuildLogPath 'build.log' | Out-Null");

            Assert.Multiple(() =>
            {
                Assert.That(rejected.ExitCode, Is.EqualTo(1));
                Assert.That(rejected.StandardError, Does.Contain("Gateway").IgnoreCase);
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Repo_project_and_deployed_product_evidence_are_exact_and_reject_stale_patch_xml()
    {
        var root = Path.Combine(Path.GetTempPath(), "GatewaySmokeProductModEvidenceTests", Guid.NewGuid().ToString("N"));
        var projectDirectory = Path.Combine(root, "mods", "Product");
        var projectPath = Path.Combine(projectDirectory, "Product.csproj");
        var sourcePatches = Path.Combine(projectDirectory, "Patches");
        var game = Path.Combine(root, "game");
        var deployed = Path.Combine(game, "Mods", "product.mod");
        var deployedAssemblies = Path.Combine(deployed, "1.6", "Assemblies");
        var deployedPatches = Path.Combine(deployed, "1.6", "Patches");
        var deployedAbout = Path.Combine(deployed, "About");
        Directory.CreateDirectory(sourcePatches);
        Directory.CreateDirectory(deployedAssemblies);
        Directory.CreateDirectory(deployedPatches);
        Directory.CreateDirectory(deployedAbout);
        try
        {
            var cleanAssembly = typeof(TestAttribute).Assembly;
            var assemblyName = cleanAssembly.GetName().Name!;
            File.WriteAllText(
                projectPath,
                "<Project><PropertyGroup><RimWorldPackageId>product.mod</RimWorldPackageId>" +
                "<AssemblyName>" + assemblyName + "</AssemblyName></PropertyGroup></Project>",
                new UTF8Encoding(false));
            File.Copy(cleanAssembly.Location, Path.Combine(deployedAssemblies, assemblyName + ".dll"));
            File.WriteAllText(Path.Combine(sourcePatches, "Probe.xml"), "<Patch><marker>source</marker></Patch>", new UTF8Encoding(false));
            File.Copy(Path.Combine(sourcePatches, "Probe.xml"), Path.Combine(deployedPatches, "Probe.xml"));
            File.WriteAllText(Path.Combine(deployedAbout, "About.xml"), "<ModMetaData />", new UTF8Encoding(false));

            var accepted = Invoke(
                root,
                "$project = Get-GatewaySmokeAdditionalModProject -RepositoryRoot $fixtureRoot -ProjectPath $projectPath -AdditionalPackageIds @('product.mod')\n" +
                "$evidence = Get-GatewaySmokeProductPackageEvidence -Project $project -RimWorldPath $gamePath -BuildLogPath 'build.log'\n" +
                "$evidence | ConvertTo-Json -Compress -Depth 8");
            File.WriteAllText(Path.Combine(deployedPatches, "Stale.xml"), "<Patch />", new UTF8Encoding(false));
            var stale = Invoke(
                root,
                "$project = Get-GatewaySmokeAdditionalModProject -RepositoryRoot $fixtureRoot -ProjectPath $projectPath -AdditionalPackageIds @('product.mod')\n" +
                "Get-GatewaySmokeProductPackageEvidence -Project $project -RimWorldPath $gamePath -BuildLogPath 'build.log' | Out-Null");

            Assert.Multiple(() =>
            {
                Assert.That(accepted.ExitCode, Is.Zero, accepted.StandardError);
                Assert.That(accepted.StandardOutput, Does.Contain("\"PackageId\":\"product.mod\""));
                Assert.That(accepted.StandardOutput, Does.Contain("\"RelativePath\":\"Patches/Probe.xml\""));
                Assert.That(accepted.StandardOutput, Does.Contain("\"Sha256\":"));
                Assert.That(stale.ExitCode, Is.EqualTo(1));
                Assert.That(stale.StandardError, Does.Contain("exact source patch set").IgnoreCase);
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static InvocationResult Invoke(string root, string operation)
    {
        var smokePath = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
        var invocationPath = Path.Combine(root, "invoke-" + Guid.NewGuid().ToString("N") + ".ps1");
        var projectPath = Path.Combine(root, "mods", "Product", "Product.csproj");
        var gamePath = Path.Combine(root, "game");
        var functions = new[]
        {
            "Test-GatewayPackageId",
            "Get-GatewaySmokeFileSha256",
            "Get-GatewaySmokeRelativePath",
            "Get-GatewaySmokeAssemblyEvidence",
            "Get-GatewaySmokeFileEvidence",
            "Get-GatewaySmokeManagedAssemblyEvidence",
            "Get-GatewaySmokeAdditionalModProject",
            "Get-GatewaySmokeProductPackageEvidence"
        };
        var invocation =
            "$ErrorActionPreference = 'Stop'\nSet-StrictMode -Version Latest\n" +
            "$tokens = $null; $parseErrors = $null\n" +
            $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
            $"$functionNames = @({string.Join(",", functions.Select(PowerShellLiteral))})\n" +
            "foreach ($functionName in $functionNames) {\n" +
            "  $functionAst = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $functionName }, $true)\n" +
            "  if ($null -eq $functionAst) { throw \"Function was not found: $functionName\" }\n" +
            "  Invoke-Expression $functionAst.Extent.Text\n}\n" +
            $"$fixtureRoot = {PowerShellLiteral(root)}\n" +
            $"$projectPath = {PowerShellLiteral(projectPath)}\n" +
            $"$gamePath = {PowerShellLiteral(gamePath)}\n" +
            "try {\n" + operation + "\nexit 0\n}\n" +
            "catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }\n";
        File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));
        return RunPowerShell(invocationPath);
    }

    private static InvocationResult InvokeWindowsPowerShell(string root, string operation)
    {
        var smokePath = Path.Combine(FindSourceRepositoryRoot(), "scripts", "Invoke-GatewaySmoke.ps1");
        var invocationPath = Path.Combine(root, "invoke-ps51-" + Guid.NewGuid().ToString("N") + ".ps1");
        var projectPath = Path.Combine(root, "mods", "Product", "Product.csproj");
        var invocation =
            "$ErrorActionPreference = 'Stop'\nSet-StrictMode -Version Latest\n" +
            "$tokens = $null; $parseErrors = $null\n" +
            $"$ast = [System.Management.Automation.Language.Parser]::ParseFile({PowerShellLiteral(smokePath)}, [ref]$tokens, [ref]$parseErrors)\n" +
            "$functionNames = @('Test-GatewayPackageId','Get-GatewaySmokeRelativePath','Get-GatewaySmokeAdditionalModProject')\n" +
            "foreach ($functionName in $functionNames) {\n" +
            "  $functionAst = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $functionName }, $true)\n" +
            "  if ($null -eq $functionAst) { throw \"Function was not found: $functionName\" }\n" +
            "  Invoke-Expression $functionAst.Extent.Text\n}\n" +
            $"$fixtureRoot = {PowerShellLiteral(root)}\n" +
            $"$projectPath = {PowerShellLiteral(projectPath)}\n" +
            "try {\n" + operation + "\nexit 0\n}\n" +
            "catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }\n";
        File.WriteAllText(invocationPath, invocation, new UTF8Encoding(false));

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{invocationPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start Windows PowerShell.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000))
        {
            process.Kill();
            throw new TimeoutException("Gateway Windows PowerShell compatibility probe timed out.");
        }
        Task.WaitAll(output, error);
        return new InvocationResult(process.ExitCode, output.Result, error.Result);
    }

    private static InvocationResult RunPowerShell(string path)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "pwsh.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{path}\"",
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
            throw new TimeoutException("Gateway product-package evidence probe timed out.");
        }

        Task.WaitAll(output, error);
        return new InvocationResult(process.ExitCode, output.Result, error.Result);
    }

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

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("X2")));
    }

    private static string PowerShellLiteral(string value) => $"'{value.Replace("'", "''")}'";

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
