using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace RimWorldDevGateway.Client;

public sealed class DotNetGatewaySourceCompiler : IGatewaySourceCompiler
{
    private const int MaximumDiagnosticCharacters = 16 * 1024;

    private readonly string dotNetPath;
    private readonly string temporaryRoot;
    private readonly TimeSpan timeout;

    public DotNetGatewaySourceCompiler(
        string dotNetPath = "dotnet",
        string? temporaryRoot = null,
        TimeSpan? timeout = null)
    {
        if (string.IsNullOrWhiteSpace(dotNetPath))
        {
            throw new ArgumentException("The dotnet executable path is required.", nameof(dotNetPath));
        }

        this.dotNetPath = dotNetPath;
        this.temporaryRoot = Path.GetFullPath(
            string.IsNullOrWhiteSpace(temporaryRoot)
                ? Path.Combine(Path.GetTempPath(), "RimWorldDevGateway.Compiler")
                : temporaryRoot);
        this.timeout = timeout ?? TimeSpan.FromSeconds(60);
        if (this.timeout <= TimeSpan.Zero || this.timeout.TotalMilliseconds > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }
    }

    public byte[] Compile(GatewaySourceCompilationRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var sourcePath = RequireFile(request.SourcePath, "Source file");
        var managedPath = RequireDirectory(request.ManagedAssembliesPath, "RimWorld managed assemblies directory");
        var contractPath = RequireFile(request.GatewayContractPath, "Gateway contract assembly");

        Directory.CreateDirectory(temporaryRoot);
        var identifier = Guid.NewGuid().ToString("N");
        var assemblyName = "RimWorldGatewaySnippet_" + identifier;
        var workspace = Path.Combine(temporaryRoot, "snippet-" + identifier);
        Directory.CreateDirectory(workspace);

        try
        {
            var copiedSourcePath = Path.Combine(workspace, "Snippet.cs");
            File.Copy(sourcePath, copiedSourcePath, false);
            var projectPath = Path.Combine(workspace, assemblyName + ".csproj");
            File.WriteAllText(
                projectPath,
                CreateProject(assemblyName, managedPath, contractPath),
                new UTF8Encoding(false));

            RunBuild(projectPath, workspace);
            var outputPath = Path.Combine(workspace, "bin", "Release", assemblyName + ".dll");
            if (!File.Exists(outputPath))
            {
                throw new GatewayClientException("The dotnet build succeeded without producing the snippet assembly.");
            }

            return File.ReadAllBytes(outputPath);
        }
        finally
        {
            if (Directory.Exists(workspace))
            {
                Directory.Delete(workspace, true);
            }
        }
    }

    private static string RequireFile(string path, string label)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new GatewayClientException(label + " path is required.");
        }

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new GatewayClientException($"{label} does not exist: '{fullPath}'.");
        }

        return fullPath;
    }

    private static string RequireDirectory(string path, string label)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new GatewayClientException(label + " path is required.");
        }

        var fullPath = Path.GetFullPath(path);
        if (!Directory.Exists(fullPath))
        {
            throw new GatewayClientException($"{label} does not exist: '{fullPath}'.");
        }

        return fullPath;
    }

    private static string CreateProject(string assemblyName, string managedPath, string contractPath)
    {
        var references = new List<string>();
        references.AddRange(Directory.EnumerateFiles(managedPath, "Assembly-CSharp*.dll"));
        references.AddRange(Directory.EnumerateFiles(managedPath, "Unity*.dll"));
        references.Add(contractPath);

        var project = new StringBuilder();
        project.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        project.AppendLine("  <PropertyGroup>");
        project.AppendLine("    <TargetFramework>net48</TargetFramework>");
        project.AppendLine("    <LangVersion>latest</LangVersion>");
        project.AppendLine("    <Nullable>enable</Nullable>");
        project.AppendLine("    <Deterministic>true</Deterministic>");
        project.AppendLine("    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>");
        project.AppendLine("    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>");
        project.Append("    <AssemblyName>").Append(Xml(assemblyName)).AppendLine("</AssemblyName>");
        project.AppendLine("  </PropertyGroup>");
        project.AppendLine("  <ItemGroup>");
        project.AppendLine("    <Compile Include=\"Snippet.cs\" />");
        foreach (var reference in references.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            project.Append("    <Reference Include=\"")
                .Append(Xml(Path.GetFileNameWithoutExtension(reference)))
                .AppendLine("\">");
            project.Append("      <HintPath>").Append(Xml(reference)).AppendLine("</HintPath>");
            project.AppendLine("      <Private>false</Private>");
            project.AppendLine("    </Reference>");
        }

        project.AppendLine("  </ItemGroup>");
        project.AppendLine("</Project>");
        return project.ToString();
    }

    private void RunBuild(string projectPath, string workspace)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = dotNetPath,
            Arguments = "build " + Quote(projectPath) + " --configuration Release --nologo --verbosity:minimal",
            WorkingDirectory = workspace,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            using var process = Process.Start(startInfo) ??
                throw new GatewayClientException("Could not start the installed dotnet SDK.");
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                process.Kill();
                process.WaitForExit();
                throw new GatewayClientException($"Snippet compilation exceeded the {timeout.TotalSeconds:0}-second limit.");
            }

            process.WaitForExit();
            var output = standardOutput.GetAwaiter().GetResult();
            var error = standardError.GetAwaiter().GetResult();
            if (process.ExitCode != 0)
            {
                var diagnostics = Bound((error + Environment.NewLine + output).Trim(), MaximumDiagnosticCharacters);
                throw new GatewayClientException(
                    string.IsNullOrEmpty(diagnostics)
                        ? $"Snippet compilation failed with exit code {process.ExitCode}."
                        : $"Snippet compilation failed with exit code {process.ExitCode}:{Environment.NewLine}{diagnostics}");
            }
        }
        catch (GatewayClientException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new GatewayClientException(
                $"Could not run the installed dotnet SDK: {Bound(exception.Message, 1024)}",
                exception);
        }
    }

    private static string Quote(string value) => '"' + value.Replace("\"", "\\\"") + '"';

    private static string Xml(string value) => SecurityElement.Escape(value) ?? string.Empty;

    private static string Bound(string value, int maximumCharacters) =>
        value.Length <= maximumCharacters ? value : value.Substring(0, maximumCharacters) + "...";
}
