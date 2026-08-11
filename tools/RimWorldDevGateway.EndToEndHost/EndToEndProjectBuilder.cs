using System.Diagnostics;
using System.Text.Json;

namespace RimWorldDevGateway.EndToEndHost;

public sealed class EndToEndBuildCommandResult
{
    public EndToEndBuildCommandResult(int exitCode, string standardOutput, string standardError)
    {
        ExitCode = exitCode;
        StandardOutput = standardOutput ?? string.Empty;
        StandardError = standardError ?? string.Empty;
    }

    public int ExitCode { get; }

    public string StandardOutput { get; }

    public string StandardError { get; }
}

public interface IEndToEndBuildCommandRunner
{
    EndToEndBuildCommandResult Run(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        TimeSpan timeout);
}

public sealed class EndToEndProjectBuildException : Exception
{
    public EndToEndProjectBuildException(string message) : base(message)
    {
    }

    public EndToEndProjectBuildException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public sealed class EndToEndProcessBuildCommandRunner : IEndToEndBuildCommandRunner
{
    public EndToEndBuildCommandResult Run(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A build executable is required.", nameof(fileName));
        }

        if (arguments is null)
        {
            throw new ArgumentNullException(nameof(arguments));
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                throw new EndToEndProjectBuildException($"Could not start build executable '{fileName}'.");
            }
        }
        catch (EndToEndProjectBuildException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new EndToEndProjectBuildException(
                $"Could not start build executable '{fileName}'.",
                exception);
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        using var cancellation = new CancellationTokenSource(timeout);
        try
        {
            process.WaitForExitAsync(cancellation.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException exception)
        {
            try
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
            }
            catch
            {
                // The timeout failure remains primary; the exact child is already gone or inaccessible.
            }

            throw new EndToEndProjectBuildException(
                $"Build command exceeded its timeout of {timeout}.",
                exception);
        }

        Task.WaitAll(outputTask, errorTask);
        return new EndToEndBuildCommandResult(
            process.ExitCode,
            outputTask.Result,
            errorTask.Result);
    }
}

public sealed class EndToEndProjectBuilder
{
    private readonly IEndToEndBuildCommandRunner commandRunner;
    private readonly string dotnetExecutable;

    public EndToEndProjectBuilder(
        IEndToEndBuildCommandRunner? commandRunner = null,
        string dotnetExecutable = "dotnet")
    {
        this.commandRunner = commandRunner ?? new EndToEndProcessBuildCommandRunner();
        this.dotnetExecutable = string.IsNullOrWhiteSpace(dotnetExecutable)
            ? throw new ArgumentException("A dotnet executable is required.", nameof(dotnetExecutable))
            : dotnetExecutable;
    }

    public IReadOnlyList<EndToEndAssemblyCandidate> Build(
        IEnumerable<EndToEndProjectRecord> projects,
        string configuration,
        TimeSpan timeoutPerCommand)
    {
        if (projects is null)
        {
            throw new ArgumentNullException(nameof(projects));
        }

        ValidateConfiguration(configuration);
        if (timeoutPerCommand <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutPerCommand));
        }

        var ordered = projects.OrderBy(project => project.ProjectPath, StringComparer.OrdinalIgnoreCase).ToArray();
        if (ordered.Length == 0)
        {
            throw new EndToEndProjectBuildException("E2E build selected zero marked projects.");
        }

        var candidates = new List<EndToEndAssemblyCandidate>();
        foreach (var project in ordered)
        {
            candidates.Add(BuildOne(project, configuration, timeoutPerCommand));
        }

        return candidates.AsReadOnly();
    }

    public IReadOnlyList<PerformanceAssemblyCandidate> BuildPerformance(
        IEnumerable<PerformanceProjectRecord> projects,
        string configuration,
        TimeSpan timeoutPerCommand)
    {
        if (projects is null) throw new ArgumentNullException(nameof(projects));
        ValidateConfiguration(configuration);
        if (timeoutPerCommand <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeoutPerCommand));

        var materialized = new List<PerformanceProjectRecord>();
        using (var enumerator = projects.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                if (materialized.Count == PerformanceDiscoveryValidator.MaximumBenchmarks)
                    throw new EndToEndProjectBuildException(
                        $"Performance build exceeds the published " +
                        $"{PerformanceDiscoveryValidator.MaximumBenchmarks}-project ceiling before any build starts.");
                materialized.Add(enumerator.Current ??
                    throw new EndToEndProjectBuildException("Performance build contains a null project record."));
            }
        }
        var ordered = materialized.OrderBy(
            project => project.ProjectPath,
            StringComparer.OrdinalIgnoreCase).ToArray();
        if (ordered.Length == 0)
            throw new EndToEndProjectBuildException("Performance build selected zero marked projects.");

        return ordered.Select(project => BuildPerformanceOne(project, configuration, timeoutPerCommand))
            .ToArray();
    }

    private PerformanceAssemblyCandidate BuildPerformanceOne(
        PerformanceProjectRecord project,
        string configuration,
        TimeSpan timeout)
    {
        var projectPath = Path.GetFullPath(project.ProjectPath);
        if (!File.Exists(projectPath))
            throw new EndToEndProjectBuildException($"Performance project does not exist: {projectPath}");
        var workingDirectory = Path.GetDirectoryName(projectPath) ??
            throw new EndToEndProjectBuildException($"Performance project has no parent directory: {projectPath}");

        var build = commandRunner.Run(dotnetExecutable, new[]
        {
            "build", projectPath, "--configuration", configuration, "--nologo", "--verbosity", "minimal"
        }, workingDirectory, timeout);
        EnsureSucceeded("performance build", projectPath, build);
        var propertiesResult = commandRunner.Run(dotnetExecutable, new[]
        {
            "msbuild", projectPath, "-nologo", "-verbosity:quiet", "-property:Configuration=" + configuration,
            "-getProperty:TargetPath", "-getProperty:AssemblyName", "-getProperty:TargetFramework"
        }, workingDirectory, timeout);
        EnsureSucceeded("performance output-property query", projectPath, propertiesResult);
        var properties = ReadProperties(projectPath, propertiesResult.StandardOutput);

        if (!StringComparer.Ordinal.Equals(properties.TargetFramework, "net480") ||
            !StringComparer.Ordinal.Equals(project.TargetFramework, "net480"))
            throw new EndToEndProjectBuildException(
                $"Performance project '{projectPath}' must target net480; declared '{project.TargetFramework}', " +
                $"evaluated '{properties.TargetFramework}'.");
        if (!StringComparer.Ordinal.Equals(properties.AssemblyName, project.AssemblyName))
            throw new EndToEndProjectBuildException(
                $"Performance project '{projectPath}' evaluated assembly name '{properties.AssemblyName}' " +
                $"instead of discovered '{project.AssemblyName}'.");

        var targetPath = Path.GetFullPath(properties.TargetPath);
        if (!File.Exists(targetPath))
            throw new EndToEndProjectBuildException(
                $"Performance project '{projectPath}' reported a missing TargetPath: {targetPath}");

        try
        {
            return new PerformanceAssemblyCandidate(
                projectPath,
                project.OwnerPackageId,
                project.AssemblyName,
                PerformanceAssemblyMetadataReader.Read(targetPath));
        }
        catch (Exception exception) when (exception is not EndToEndProjectBuildException)
        {
            throw new EndToEndProjectBuildException(
                $"Could not read compiled performance metadata for '{projectPath}'.",
                exception);
        }
    }

    private EndToEndAssemblyCandidate BuildOne(
        EndToEndProjectRecord project,
        string configuration,
        TimeSpan timeout)
    {
        var projectPath = Path.GetFullPath(project.ProjectPath);
        if (!File.Exists(projectPath))
        {
            throw new EndToEndProjectBuildException($"E2E project does not exist: {projectPath}");
        }

        var workingDirectory = Path.GetDirectoryName(projectPath) ??
            throw new EndToEndProjectBuildException($"E2E project has no parent directory: {projectPath}");
        var buildArguments = new[]
        {
            "build",
            projectPath,
            "--configuration",
            configuration,
            "--nologo",
            "--verbosity",
            "minimal"
        };
        var build = commandRunner.Run(dotnetExecutable, buildArguments, workingDirectory, timeout);
        EnsureSucceeded("build", projectPath, build);

        var propertyArguments = new[]
        {
            "msbuild",
            projectPath,
            "-nologo",
            "-verbosity:quiet",
            "-property:Configuration=" + configuration,
            "-getProperty:TargetPath",
            "-getProperty:AssemblyName",
            "-getProperty:TargetFramework"
        };
        var propertiesResult = commandRunner.Run(
            dotnetExecutable,
            propertyArguments,
            workingDirectory,
            timeout);
        EnsureSucceeded("output-property query", projectPath, propertiesResult);
        var properties = ReadProperties(projectPath, propertiesResult.StandardOutput);

        if (!StringComparer.Ordinal.Equals(properties.TargetFramework, "net480") ||
            !StringComparer.Ordinal.Equals(project.TargetFramework, "net480"))
        {
            throw new EndToEndProjectBuildException(
                $"E2E project '{projectPath}' must target net480; declared '{project.TargetFramework}', " +
                $"evaluated '{properties.TargetFramework}'.");
        }

        if (!StringComparer.Ordinal.Equals(properties.AssemblyName, project.AssemblyName))
        {
            throw new EndToEndProjectBuildException(
                $"E2E project '{projectPath}' evaluated assembly name '{properties.AssemblyName}' " +
                $"instead of discovered '{project.AssemblyName}'.");
        }

        var targetPath = Path.GetFullPath(properties.TargetPath);
        if (!File.Exists(targetPath))
        {
            throw new EndToEndProjectBuildException(
                $"E2E project '{projectPath}' reported a missing TargetPath: {targetPath}");
        }

        EndToEndAssemblyMetadata metadata;
        try
        {
            metadata = EndToEndAssemblyMetadataReader.Read(targetPath);
        }
        catch (Exception exception) when (exception is not EndToEndProjectBuildException)
        {
            throw new EndToEndProjectBuildException(
                $"Could not read compiled E2E metadata for '{projectPath}'.",
                exception);
        }

        return new EndToEndAssemblyCandidate(
            projectPath,
            project.OwnerPackageId,
            project.AssemblyName,
            metadata);
    }

    private static EvaluatedProperties ReadProperties(string projectPath, string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var properties = document.RootElement.GetProperty("Properties");
            return new EvaluatedProperties(
                properties.GetProperty("TargetPath").GetString() ?? string.Empty,
                properties.GetProperty("AssemblyName").GetString() ?? string.Empty,
                properties.GetProperty("TargetFramework").GetString() ?? string.Empty);
        }
        catch (Exception exception)
        {
            throw new EndToEndProjectBuildException(
                $"MSBuild returned an invalid output-property document for '{projectPath}'.",
                exception);
        }
    }

    private static void EnsureSucceeded(
        string operation,
        string projectPath,
        EndToEndBuildCommandResult result)
    {
        if (result.ExitCode == 0)
        {
            return;
        }

        throw new EndToEndProjectBuildException(
            $"E2E {operation} failed for '{projectPath}' with exit code {result.ExitCode}." +
            Environment.NewLine +
            "stderr:" + Environment.NewLine + result.StandardError + Environment.NewLine +
            "stdout:" + Environment.NewLine + result.StandardOutput);
    }

    private static void ValidateConfiguration(string configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration) ||
            configuration.Any(character =>
                !(char.IsLetterOrDigit(character) || character == '.' || character == '_' || character == '-')))
        {
            throw new EndToEndProjectBuildException(
                $"Invalid E2E build configuration '{configuration}'.");
        }
    }

    private sealed class EvaluatedProperties
    {
        public EvaluatedProperties(string targetPath, string assemblyName, string targetFramework)
        {
            TargetPath = targetPath;
            AssemblyName = assemblyName;
            TargetFramework = targetFramework;
        }

        public string TargetPath { get; }

        public string AssemblyName { get; }

        public string TargetFramework { get; }
    }
}
