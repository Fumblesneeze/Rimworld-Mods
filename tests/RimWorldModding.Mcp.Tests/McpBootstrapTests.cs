using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class McpBootstrapTests
{
    [Test]
    public void RepositoryConfigurations_ProjectTheSameBootstrapCommand()
    {
        var root = TestRepository.FindRoot();
        var codexConfig = File.ReadAllText(Path.Combine(root, ".codex", "config.toml"));
        var portableConfigPath = Path.Combine(root, ".mcp.json");
        Assert.That(File.Exists(portableConfigPath), Is.True, "The portable MCP projection is missing.");

        using var portableConfig = JsonDocument.Parse(File.ReadAllText(portableConfigPath));
        var server = portableConfig.RootElement.GetProperty("mcpServers").GetProperty("rimworld_modding");
        var portableCommand = server.GetProperty("command").GetString();
        var portableArguments = server.GetProperty("args").EnumerateArray().Select(item => item.GetString()).ToArray();

        var commandMatch = Regex.Match(codexConfig, "(?m)^command\\s*=\\s*\\\"(?<value>[^\\\"]+)\\\"\\r?$");
        var argumentsMatch = Regex.Match(codexConfig, "(?m)^args\\s*=\\s*\\[(?<value>[^\\]]+)\\]\\r?$");
        Assert.That(codexConfig, Does.Contain("[mcp_servers.rimworld_modding]"));
        Assert.That(codexConfig, Does.Not.Contain("# [mcp_servers.rimworld_modding]"));
        Assert.That(commandMatch.Success, Is.True);
        Assert.That(argumentsMatch.Success, Is.True);

        var codexArguments = Regex.Matches(argumentsMatch.Groups["value"].Value, "\\\"(?<value>[^\\\"]*)\\\"")
            .Select(match => match.Groups["value"].Value)
            .ToArray();
        Assert.That(commandMatch.Groups["value"].Value, Is.EqualTo(portableCommand));
        Assert.That(codexArguments, Is.EqualTo(portableArguments));
        Assert.That(portableCommand, Is.EqualTo("pwsh"));
        Assert.That(portableArguments, Is.EqualTo(new[]
        {
            "-NoLogo", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File",
            ".codex/Start-RimWorldModdingMcp.ps1"
        }));
        Assert.That(codexConfig, Does.Not.Match("(?m)^cwd\\s*="),
            "Codex must use its logical workspace-root fallback rather than an OS-process-relative cwd.");
        Assert.That(codexConfig, Does.Match("(?m)^required\\s*=\\s*true\\r?$"));
        Assert.That(server.GetProperty("cwd").GetString(), Is.EqualTo("."));
        Assert.That(File.Exists(Path.Combine(root, ".codex", "Start-RimWorldModdingMcp.ps1")), Is.True);
    }

    [Test]
    [Timeout(120_000)]
    public async Task ColdConcurrentStarts_ReturnProtocolCleanInitializeResponses()
    {
        using var fixture = BootstrapRepositoryFixture.Create();
        var root = fixture.Root;
        var script = Path.Combine(root, ".codex", "Start-RimWorldModdingMcp.ps1");
        var cacheRoot = Path.Combine(root, "artifacts", "HostTools", "RimWorldModding.Mcp", "SourceBootstrap");

        var clients = Enumerable.Range(0, 6)
            .Select(_ => StartClient(root, script))
            .ToList();

        try
        {
            const string initialize = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"bootstrap-test\",\"version\":\"1.0\"}}}";
            foreach (var client in clients)
            {
                await client.Process.StandardInput.WriteLineAsync(initialize);
                await client.Process.StandardInput.FlushAsync();
            }

            var responses = await Task.WhenAll(clients.Select(async client =>
            {
                var line = await client.Process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(90));
                Assert.That(line, Is.Not.Null.And.Not.Empty, await ReadFailureAsync(client));
                using var payload = JsonDocument.Parse(line!);
                Assert.That(payload.RootElement.GetProperty("id").GetInt32(), Is.EqualTo(1));
                Assert.That(
                    payload.RootElement.GetProperty("result").GetProperty("serverInfo").GetProperty("name").GetString(),
                    Is.EqualTo("rimworld-modding"));
                return line;
            }));

            Assert.That(responses, Has.Length.EqualTo(6));
            var publishedEntries = Directory.GetDirectories(Path.Combine(cacheRoot, "cache"));
            Assert.That(publishedEntries, Has.Length.EqualTo(1));
            var manifestPath = Path.Combine(publishedEntries[0], "bootstrap-manifest.json");
            Assert.That(File.Exists(manifestPath), Is.True);
            using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
            var inputs = manifest.RootElement.GetProperty("inputs").EnumerateArray()
                .Select(item => item.GetProperty("path").GetString())
                .ToArray();
            Assert.That(inputs, Does.Contain("global.json"));
            Assert.That(inputs, Does.Contain(".codex/Start-RimWorldModdingMcp.ps1"));
            Assert.That(inputs, Does.Contain("tools/RimWorldModding.Mcp/RimWorldModding.Mcp.csproj"));
        }
        finally
        {
            foreach (var client in clients)
            {
                try { client.Process.StandardInput.Close(); } catch { }
            }

            foreach (var client in clients)
            {
                if (!client.Process.WaitForExit(5_000))
                {
                    client.Process.Kill(entireProcessTree: true);
                    client.Process.WaitForExit();
                }

                client.Process.Dispose();
            }
        }
    }

    [Test]
    [Timeout(120_000)]
    public async Task CancelledColdBuild_LeavesNoPublishedEntryAndRemainsRetryable()
    {
        using var fixture = BootstrapRepositoryFixture.Create();
        var root = fixture.Root;
        var script = Path.Combine(root, ".codex", "Start-RimWorldModdingMcp.ps1");
        var cacheRoot = Path.Combine(root, "artifacts", "HostTools", "RimWorldModding.Mcp", "SourceBootstrap");
        var cancellationMarker = fixture.InstallCancellationBarrier();

        var cancelled = StartClient(root, script);
        try
        {
            var barrierObserved = await WaitUntilAsync(
                () => File.Exists(cancellationMarker),
                TimeSpan.FromSeconds(20));
            Assert.That(barrierObserved, Is.True, "The cold source build never reached the pre-publication barrier.");

            cancelled.Process.Kill(entireProcessTree: true);
            cancelled.Process.WaitForExit();
            Assert.That(
                Directory.Exists(Path.Combine(cacheRoot, "cache"))
                    ? Directory.GetDirectories(Path.Combine(cacheRoot, "cache"))
                    : Array.Empty<string>(),
                Is.Empty,
                "A cancelled staged build must not publish a cache entry.");
        }
        finally
        {
            if (!cancelled.Process.HasExited)
            {
                cancelled.Process.Kill(entireProcessTree: true);
                cancelled.Process.WaitForExit();
            }

            cancelled.Process.Dispose();
        }

        fixture.RemoveCancellationBarrier();
        var retry = StartClient(root, script);
        try
        {
            var response = await InitializeAsync(retry);
            using var payload = JsonDocument.Parse(response);
            Assert.That(payload.RootElement.GetProperty("id").GetInt32(), Is.EqualTo(1));
            Assert.That(Directory.GetDirectories(Path.Combine(cacheRoot, "cache")), Has.Length.EqualTo(1));
            Assert.That(Directory.GetDirectories(Path.Combine(cacheRoot, "staging")), Is.Empty,
                "A retry must scavenge staging abandoned by the killed bootstrap.");
        }
        finally
        {
            try { retry.Process.StandardInput.Close(); } catch { }
            if (!retry.Process.WaitForExit(5_000))
            {
                retry.Process.Kill(entireProcessTree: true);
                retry.Process.WaitForExit();
            }

            retry.Process.Dispose();
        }
    }

    [Test]
    [Timeout(120_000)]
    public async Task FailedColdBuild_LeavesNoPublishedEntryAndRemainsRetryable()
    {
        using var fixture = BootstrapRepositoryFixture.Create();
        var root = fixture.Root;
        var script = Path.Combine(root, ".codex", "Start-RimWorldModdingMcp.ps1");
        var cacheRoot = Path.Combine(root, "artifacts", "HostTools", "RimWorldModding.Mcp", "SourceBootstrap");
        var failureSource = Path.Combine(root, "tools", "RimWorldModding.Mcp", "IntentionalBuildFailure.cs");
        File.WriteAllText(failureSource, "this is intentionally not valid C#;");

        var failed = StartClient(root, script);
        try
        {
            Assert.That(failed.Process.WaitForExit(90_000), Is.True, "The intentionally broken build did not exit.");
            Assert.That(failed.Process.ExitCode, Is.Not.EqualTo(0));
            Assert.That(await failed.Process.StandardOutput.ReadToEndAsync(), Is.Empty,
                "A failed bootstrap must keep protocol stdout empty.");
            Assert.That(
                Directory.Exists(Path.Combine(cacheRoot, "cache"))
                    ? Directory.GetDirectories(Path.Combine(cacheRoot, "cache"))
                    : Array.Empty<string>(),
                Is.Empty,
                "A failed build must not publish a cache entry.");
            Assert.That(Directory.GetDirectories(Path.Combine(cacheRoot, "staging")), Is.Empty,
                "A normally failed build must clean its isolated staging directory.");
        }
        finally
        {
            if (!failed.Process.HasExited)
            {
                failed.Process.Kill(entireProcessTree: true);
                failed.Process.WaitForExit();
            }

            failed.Process.Dispose();
        }

        File.Delete(failureSource);
        var retry = StartClient(root, script);
        try
        {
            var response = await InitializeAsync(retry);
            using var payload = JsonDocument.Parse(response);
            Assert.That(payload.RootElement.GetProperty("id").GetInt32(), Is.EqualTo(1));
            Assert.That(Directory.GetDirectories(Path.Combine(cacheRoot, "cache")), Has.Length.EqualTo(1));
        }
        finally
        {
            try { retry.Process.StandardInput.Close(); } catch { }
            if (!retry.Process.WaitForExit(5_000))
            {
                retry.Process.Kill(entireProcessTree: true);
                retry.Process.WaitForExit();
            }

            retry.Process.Dispose();
        }
    }

    [Test]
    [Timeout(120_000)]
    public async Task CorruptedPublishedEntry_IsQuarantinedAndRebuilt()
    {
        using var fixture = BootstrapRepositoryFixture.Create();
        var root = fixture.Root;
        var script = Path.Combine(root, ".codex", "Start-RimWorldModdingMcp.ps1");
        var cacheRoot = Path.Combine(root, "artifacts", "HostTools", "RimWorldModding.Mcp", "SourceBootstrap");

        var initial = StartClient(root, script);
        try
        {
            _ = await InitializeAsync(initial);
        }
        finally
        {
            try { initial.Process.StandardInput.Close(); } catch { }
            if (!initial.Process.WaitForExit(5_000))
            {
                initial.Process.Kill(entireProcessTree: true);
                initial.Process.WaitForExit();
            }

            initial.Process.Dispose();
        }

        var published = Directory.GetDirectories(Path.Combine(cacheRoot, "cache")).Single();
        File.AppendAllText(Path.Combine(published, "RimWorldModding.Mcp.dll"), "intentional corruption");

        var repaired = StartClient(root, script);
        try
        {
            var response = await InitializeAsync(repaired);
            using var payload = JsonDocument.Parse(response);
            Assert.That(payload.RootElement.GetProperty("id").GetInt32(), Is.EqualTo(1));
            Assert.That(Directory.GetDirectories(Path.Combine(cacheRoot, "cache")), Has.Length.EqualTo(1));
            Assert.That(Directory.GetDirectories(cacheRoot, "invalid-*"), Has.Length.EqualTo(1));
        }
        finally
        {
            try { repaired.Process.StandardInput.Close(); } catch { }
            if (!repaired.Process.WaitForExit(5_000))
            {
                repaired.Process.Kill(entireProcessTree: true);
                repaired.Process.WaitForExit();
            }

            repaired.Process.Dispose();
        }
    }

    [Test]
    [Timeout(120_000)]
    public async Task TimedOutColdBuild_IsStoppedAndRemainsRetryable()
    {
        using var fixture = BootstrapRepositoryFixture.Create();
        var root = fixture.Root;
        var script = Path.Combine(root, ".codex", "Start-RimWorldModdingMcp.ps1");
        var cacheRoot = Path.Combine(root, "artifacts", "HostTools", "RimWorldModding.Mcp", "SourceBootstrap");
        var barrierMarker = fixture.InstallCancellationBarrier();
        var timedOut = StartClient(root, script, buildTimeoutSeconds: 8);
        try
        {
            Assert.That(await WaitUntilAsync(() => File.Exists(barrierMarker), TimeSpan.FromSeconds(20)), Is.True);
            Assert.That(timedOut.Process.WaitForExit(15_000), Is.True, "The bounded build did not stop.");
            Assert.That(timedOut.Process.ExitCode, Is.Not.EqualTo(0));
            Assert.That(await timedOut.Process.StandardOutput.ReadToEndAsync(), Is.Empty);
            Assert.That(await timedOut.StandardError, Does.Contain("exceeded its 8-second bound"));
            Assert.That(
                Directory.Exists(Path.Combine(cacheRoot, "cache"))
                    ? Directory.GetDirectories(Path.Combine(cacheRoot, "cache"))
                    : Array.Empty<string>(),
                Is.Empty);
            Assert.That(Directory.GetDirectories(Path.Combine(cacheRoot, "staging")), Is.Empty);
        }
        finally
        {
            if (!timedOut.Process.HasExited)
            {
                timedOut.Process.Kill(entireProcessTree: true);
                timedOut.Process.WaitForExit();
            }

            timedOut.Process.Dispose();
        }

        fixture.RemoveCancellationBarrier();
        var retry = StartClient(root, script);
        try
        {
            _ = await InitializeAsync(retry);
            Assert.That(Directory.GetDirectories(Path.Combine(cacheRoot, "cache")), Has.Length.EqualTo(1));
        }
        finally
        {
            try { retry.Process.StandardInput.Close(); } catch { }
            if (!retry.Process.WaitForExit(5_000))
            {
                retry.Process.Kill(entireProcessTree: true);
                retry.Process.WaitForExit();
            }

            retry.Process.Dispose();
        }
    }

    [Test]
    [Timeout(120_000)]
    public async Task FingerprintedInputChangesDuringBuild_RejectsPublicationAndRemainsRetryable()
    {
        using var fixture = BootstrapRepositoryFixture.Create();
        var root = fixture.Root;
        var script = Path.Combine(root, ".codex", "Start-RimWorldModdingMcp.ps1");
        var cacheRoot = Path.Combine(root, "artifacts", "HostTools", "RimWorldModding.Mcp", "SourceBootstrap");
        var barrierMarker = fixture.InstallCancellationBarrier();
        var changed = StartClient(root, script);
        try
        {
            Assert.That(await WaitUntilAsync(() => File.Exists(barrierMarker), TimeSpan.FromSeconds(20)), Is.True);
            File.WriteAllText(
                Path.Combine(root, "tools", "RimWorldModding.Mcp", "FingerprintMutation.cs"),
                "namespace RimWorldModding.Mcp; internal static class FingerprintMutation { }");
            fixture.ReleaseBuildBarrier();

            Assert.That(changed.Process.WaitForExit(30_000), Is.True);
            Assert.That(changed.Process.ExitCode, Is.Not.EqualTo(0));
            Assert.That(await changed.Process.StandardOutput.ReadToEndAsync(), Is.Empty);
            Assert.That(await changed.StandardError, Does.Contain("inputs changed while the bootstrap was building"));
            Assert.That(
                Directory.Exists(Path.Combine(cacheRoot, "cache"))
                    ? Directory.GetDirectories(Path.Combine(cacheRoot, "cache"))
                    : Array.Empty<string>(),
                Is.Empty);
            Assert.That(Directory.GetDirectories(Path.Combine(cacheRoot, "staging")), Is.Empty);
        }
        finally
        {
            if (!changed.Process.HasExited)
            {
                changed.Process.Kill(entireProcessTree: true);
                changed.Process.WaitForExit();
            }

            changed.Process.Dispose();
        }

        fixture.RemoveCancellationBarrier();
        var retry = StartClient(root, script);
        try
        {
            _ = await InitializeAsync(retry);
            Assert.That(Directory.GetDirectories(Path.Combine(cacheRoot, "cache")), Has.Length.EqualTo(1));
        }
        finally
        {
            try { retry.Process.StandardInput.Close(); } catch { }
            if (!retry.Process.WaitForExit(5_000))
            {
                retry.Process.Kill(entireProcessTree: true);
                retry.Process.WaitForExit();
            }

            retry.Process.Dispose();
        }
    }

    [Test]
    [Timeout(30_000)]
    public async Task ReparsePointInCacheAncestor_IsRejectedBeforeAnyExternalWrite()
    {
        using var fixture = BootstrapRepositoryFixture.Create();
        var sentinel = fixture.InstallArtifactsJunction();
        var client = StartClient(fixture.Root, Path.Combine(fixture.Root, ".codex", "Start-RimWorldModdingMcp.ps1"));
        try
        {
            Assert.That(client.Process.WaitForExit(20_000), Is.True);
            Assert.That(client.Process.ExitCode, Is.Not.EqualTo(0));
            Assert.That(await client.Process.StandardOutput.ReadToEndAsync(), Is.Empty);
            Assert.That(await client.StandardError, Does.Contain("reparse point"));
            Assert.That(File.Exists(sentinel), Is.True, "The external junction target must remain untouched.");
            Assert.That(Directory.Exists(Path.Combine(Path.GetDirectoryName(sentinel)!, "HostTools")), Is.False,
                "The bootstrap must reject the junction before creating cache children in its target.");
        }
        finally
        {
            if (!client.Process.HasExited)
            {
                client.Process.Kill(entireProcessTree: true);
                client.Process.WaitForExit();
            }

            client.Process.Dispose();
        }
    }

    private static BootstrapClient StartClient(string root, string script, int? buildTimeoutSeconds = null)
    {
        var start = new ProcessStartInfo
        {
            FileName = "pwsh",
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        start.ArgumentList.Add("-NoLogo");
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-NonInteractive");
        start.ArgumentList.Add("-ExecutionPolicy");
        start.ArgumentList.Add("Bypass");
        start.ArgumentList.Add("-File");
        start.ArgumentList.Add(script);
        if (buildTimeoutSeconds is not null)
        {
            start.ArgumentList.Add("-BuildTimeoutSeconds");
            start.ArgumentList.Add(buildTimeoutSeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start MCP bootstrap client.");
        return new BootstrapClient(process, process.StandardError.ReadToEndAsync());
    }

    private static async Task<string> InitializeAsync(BootstrapClient client)
    {
        const string initialize = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"bootstrap-test\",\"version\":\"1.0\"}}}";
        await client.Process.StandardInput.WriteLineAsync(initialize);
        await client.Process.StandardInput.FlushAsync();
        var line = await client.Process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(90));
        Assert.That(line, Is.Not.Null.And.Not.Empty, await ReadFailureAsync(client));
        return line!;
    }

    private static async Task<bool> WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (predicate()) return true;
            await Task.Delay(25);
        }

        return predicate();
    }

    private static async Task<string> ReadFailureAsync(BootstrapClient client)
    {
        if (!client.Process.HasExited)
        {
            return "MCP bootstrap returned no initialize response.";
        }

        return $"MCP bootstrap exited {client.Process.ExitCode}: {await client.StandardError}";
    }

    private sealed record BootstrapClient(Process Process, Task<string> StandardError);

    private sealed class BootstrapRepositoryFixture : IDisposable
    {
        private string? _originalTargets;
        private string? _cancellationMarker;
        private string? _barrierRelease;
        private string? _artifactsJunctionTarget;

        private BootstrapRepositoryFixture(string root)
        {
            Root = root;
        }

        public string Root { get; }

        public string InstallCancellationBarrier()
        {
            var targetsPath = Path.Combine(Root, "Directory.Build.targets");
            _originalTargets = File.Exists(targetsPath) ? File.ReadAllText(targetsPath) : null;
            _cancellationMarker = Path.Combine(Root, "bootstrap-cancel-ready");
            _barrierRelease = Path.Combine(Root, "bootstrap-barrier-release");
            var existing = _originalTargets ?? "<Project></Project>";
            var insertion = """
  <Target Name="McpBootstrapTestCancellationBarrier" BeforeTargets="CoreCompile">
    <WriteLinesToFile File="$(MSBuildThisFileDirectory)bootstrap-cancel-ready" Lines="ready" Overwrite="true" />
    <Exec Command="powershell.exe -NoLogo -NoProfile -NonInteractive -Command &quot;while (-not (Test-Path -LiteralPath '$(MSBuildThisFileDirectory)bootstrap-barrier-release')) { Start-Sleep -Milliseconds 50 }&quot;" />
  </Target>
""";
            File.WriteAllText(targetsPath, existing.Replace("</Project>", insertion + "</Project>", StringComparison.Ordinal));
            return _cancellationMarker;
        }

        public void ReleaseBuildBarrier()
        {
            if (_barrierRelease is null)
            {
                throw new InvalidOperationException("No build barrier is installed.");
            }

            File.WriteAllText(_barrierRelease, "release");
        }

        public void RemoveCancellationBarrier()
        {
            if (_cancellationMarker is null)
            {
                return;
            }

            var targetsPath = Path.Combine(Root, "Directory.Build.targets");
            if (_originalTargets is null)
            {
                File.Delete(targetsPath);
            }
            else
            {
                File.WriteAllText(targetsPath, _originalTargets);
            }

            File.Delete(_cancellationMarker);
            if (_barrierRelease is not null)
            {
                File.Delete(_barrierRelease);
            }
            _cancellationMarker = null;
            _barrierRelease = null;
            _originalTargets = null;
        }

        public string InstallArtifactsJunction()
        {
            var junction = Path.Combine(Root, "artifacts");
            _artifactsJunctionTarget = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"McpBootstrapExternal-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_artifactsJunctionTarget);
            var sentinel = Path.Combine(_artifactsJunctionTarget, "sentinel.txt");
            File.WriteAllText(sentinel, "must remain");

            var start = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            start.ArgumentList.Add("/d");
            start.ArgumentList.Add("/c");
            start.ArgumentList.Add("mklink");
            start.ArgumentList.Add("/J");
            start.ArgumentList.Add(junction);
            start.ArgumentList.Add(_artifactsJunctionTarget);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not create test junction.");
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.That(process.ExitCode, Is.EqualTo(0), $"Could not create test junction: {output} {error}");
            Assert.That((File.GetAttributes(junction) & FileAttributes.ReparsePoint) != 0, Is.True);
            return sentinel;
        }

        public static BootstrapRepositoryFixture Create()
        {
            var sourceRoot = TestRepository.FindRoot();
            var fixtureRoot = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"McpBootstrapRepository-{Guid.NewGuid():N}");
            Directory.CreateDirectory(fixtureRoot);

            CopyFile(sourceRoot, fixtureRoot, "AGENTS.md");
            CopyFile(sourceRoot, fixtureRoot, "global.json");
            CopyOptionalFile(sourceRoot, fixtureRoot, "Directory.Build.props");
            CopyOptionalFile(sourceRoot, fixtureRoot, "Directory.Build.targets");
            CopyOptionalFile(sourceRoot, fixtureRoot, "Directory.Packages.props");
            CopyOptionalFile(sourceRoot, fixtureRoot, "NuGet.config");
            CopyFile(sourceRoot, fixtureRoot, Path.Combine(".codex", "Start-RimWorldModdingMcp.ps1"));
            Directory.CreateDirectory(Path.Combine(fixtureRoot, "mods"));
            Directory.CreateDirectory(Path.Combine(fixtureRoot, "openspec"));

            var sourceProject = Path.Combine(sourceRoot, "tools", "RimWorldModding.Mcp");
            var destinationProject = Path.Combine(fixtureRoot, "tools", "RimWorldModding.Mcp");
            foreach (var source in Directory.EnumerateFiles(sourceProject, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(sourceProject, source);
                var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (segments.Contains("bin", StringComparer.OrdinalIgnoreCase) ||
                    segments.Contains("obj", StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var destination = Path.Combine(destinationProject, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(source, destination);
            }

            return new BootstrapRepositoryFixture(fixtureRoot);
        }

        public void Dispose()
        {
            RemoveCancellationBarrier();
            var junction = Path.Combine(Root, "artifacts");
            if (Directory.Exists(junction) &&
                (File.GetAttributes(junction) & FileAttributes.ReparsePoint) != 0)
            {
                Directory.Delete(junction);
            }

            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }

            if (_artifactsJunctionTarget is not null && Directory.Exists(_artifactsJunctionTarget))
            {
                Directory.Delete(_artifactsJunctionTarget, recursive: true);
            }
        }

        private static void CopyOptionalFile(string sourceRoot, string fixtureRoot, string relative)
        {
            if (File.Exists(Path.Combine(sourceRoot, relative)))
            {
                CopyFile(sourceRoot, fixtureRoot, relative);
            }
        }

        private static void CopyFile(string sourceRoot, string fixtureRoot, string relative)
        {
            var destination = Path.Combine(fixtureRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(Path.Combine(sourceRoot, relative), destination);
        }
    }
}
