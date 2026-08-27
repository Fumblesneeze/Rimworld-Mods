using System.Security.Cryptography;
using System.Text.Json;

namespace RimWorldModding.Mcp;

public sealed record SubscriberVerificationResult(
    string Status,
    string PackageId,
    string PublishedFileId,
    string WorkshopPackagePath,
    string LoadedPackagePath,
    string EvidenceRoot,
    IReadOnlyList<string> Screenshots,
    string ExpectedObservation,
    string? LocalPackageBackupPath,
    bool LocalPackageRestored,
    string NormalModListBeforeSha256,
    string NormalModListAfterSha256);

internal sealed record SubscriberRecoveryRecord(
    string Schema,
    string PackageId,
    string LocalPackagePath,
    string BackupPath,
    string NormalConfigPath,
    string NormalConfigSha256,
    string GatewayRunId);

public sealed class SubscriberVerifier(string repositoryRoot)
{
    private readonly string _repositoryRoot = RepositoryRoot.Resolve(repositoryRoot);

    public async Task<SubscriberVerificationResult> VerifyAsync(
        ReleaseProfile profile,
        SubscriberVerificationManifest manifest,
        string publishedFileId,
        string workshopPackagePath,
        CancellationToken cancellationToken)
    {
        var plan = SubscriberVerificationPlan.Create(
            _repositoryRoot, profile, manifest, publishedFileId, workshopPackagePath);
        await RecoverInterruptedAsync(profile, cancellationToken);
        RefuseRunningRimWorld();
        if (!Directory.Exists(plan.WorkshopPackagePath))
            throw new InvalidOperationException("Subscribed Workshop package is not installed.");
        return plan.Engine switch
        {
            "gateway-automation" => await VerifyGatewayAsync(profile, publishedFileId, plan, cancellationToken),
            "powershell-subscriber" => await VerifyPowerShellAsync(profile, publishedFileId, plan, cancellationToken),
            _ => throw new InvalidOperationException($"Subscriber verification engine '{plan.Engine}' is unsupported.")
        };
    }

    private async Task<SubscriberVerificationResult> VerifyGatewayAsync(
        ReleaseProfile profile,
        string publishedFileId,
        SubscriberVerificationPlan plan,
        CancellationToken cancellationToken)
    {
        var modsRoot = @"F:\Steam\steamapps\common\RimWorld\Mods";
        var localPackage = Path.Combine(modsRoot, profile.PackageId);
        if (!Directory.Exists(localPackage))
            throw new InvalidOperationException("The user's local product copy is missing before subscriber isolation.");
        var normalConfig = ModListEditor.DefaultConfigPath();
        var normalBefore = Hash(normalConfig);
        var runId = NewRunId();
        var evidenceRoot = PrepareEvidenceRoot(profile.PackageId, runId, createDirectory: true);
        var recoveryRoot = LocalModInstaller.RecoveryRoot(modsRoot);
        Directory.CreateDirectory(recoveryRoot);
        var backup = Path.Combine(recoveryRoot, $"subscriber-{profile.PackageId}-{runId}");
        if (Directory.Exists(backup)) throw new InvalidOperationException("Subscriber backup path already exists.");
        var recoveryPath = RecoveryPath(profile.PackageId);
        var leasedRun = RunLeaseManager.ReserveRunId();
        WriteRecovery(recoveryPath, new SubscriberRecoveryRecord(
            "RimWorldModdingMcp/SubscriberRecovery/v1", profile.PackageId, localPackage, backup,
            normalConfig, normalBefore, leasedRun));
        Directory.Move(localPackage, backup);

        var gatewayStopped = false;
        var cleanupCompleted = false;
        var restored = false;
        var screenshots = new List<string>();
        var loadedPackagePath = "";
        GatewayWorkshopClient? client = null;
        try
        {
            var manager = new RunLeaseManager(_repositoryRoot);
            var workflowDeadline = DateTimeOffset.UtcNow.AddSeconds(plan.TimeoutSeconds);
            var start = manager.StartGateway(plan.PackageIds, Array.Empty<string>(), plan.TimeoutSeconds, leasedRun);
            leasedRun = start.RunId;
            var ready = await WaitReadyAsync(
                manager, start.RunId, workflowDeadline, cancellationToken);
            client = new GatewayWorkshopClient(_repositoryRoot, ready.GatewayManifestPath!, ready.GameProcessId!.Value);
            await client.RegisterSourceAsync(plan.Source!, plan.EntryType!, cancellationToken);
            await client.WaitForPlayableMapAsync(workflowDeadline, cancellationToken);
            foreach (var step in plan.Steps)
            {
                var arguments = new Dictionary<string, object?> { ["operation"] = step.Operation };
                if (step.Operation == "arrange") arguments["expectedWorkshopPath"] = plan.WorkshopPackagePath;
                var response = await client.InvokeAutomationAsync(plan.Automation!, arguments, cancellationToken);
                var status = GatewayWorkshopClient.String(response, "status");
                if (!string.Equals(status, step.ExpectedStatus, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"Subscriber operation '{step.Operation}' returned '{status}', expected '{step.ExpectedStatus}'.");
                if (step.Operation == "arrange")
                {
                    loadedPackagePath = GatewayWorkshopClient.String(response, "loadedPackagePath");
                    RequireExactPath(loadedPackagePath, plan.WorkshopPackagePath);
                }
                if (step.Screenshot is not null)
                {
                    var screenshot = ContainedEvidencePath(evidenceRoot, step.Screenshot);
                    await client.CaptureScreenshotAsync(screenshot, cancellationToken);
                    screenshots.Add(screenshot);
                }
                if (step.Operation == "cleanup") cleanupCompleted = true;
            }
            if (string.IsNullOrWhiteSpace(loadedPackagePath))
                throw new InvalidOperationException("Subscriber workflow did not report its loaded package path.");
            _ = await manager.CancelAsync(start.RunId, cancellationToken);
            gatewayStopped = true;
        }
        finally
        {
            if (client is not null && !cleanupCompleted)
            {
                try
                {
                    _ = await client.InvokeAutomationAsync(
                        plan.Automation!, new Dictionary<string, object?> { ["operation"] = "cleanup" }, CancellationToken.None);
                }
                catch { /* exact run evidence and restoration state retain the primary failure */ }
            }
            if (leasedRun is not null && !gatewayStopped)
            {
                try { _ = await new RunLeaseManager(_repositoryRoot).CancelAsync(leasedRun, CancellationToken.None); }
                catch { /* exact run lease remains available for recovery */ }
            }
            if (!Directory.Exists(localPackage) && Directory.Exists(backup))
            {
                Directory.Move(backup, localPackage);
                restored = true;
            }
            var normalAfter = Hash(normalConfig);
            File.WriteAllText(
                Path.Combine(evidenceRoot, "local-restoration.txt"),
                $"localPackage={localPackage}{Environment.NewLine}restored={restored}{Environment.NewLine}" +
                $"normalModsConfigBefore={normalBefore}{Environment.NewLine}normalModsConfigAfter={normalAfter}{Environment.NewLine}");
            if (!string.Equals(normalBefore, normalAfter, StringComparison.Ordinal))
                throw new InvalidOperationException("Subscriber verification changed the user's normal ModsConfig.xml.");
            if (!restored || !Directory.Exists(localPackage) || Directory.Exists(backup))
                throw new InvalidOperationException("The user's local product package was not restored after subscriber verification.");
            File.Delete(recoveryPath);
        }

        return new SubscriberVerificationResult(
            "passed", profile.PackageId, publishedFileId, plan.WorkshopPackagePath, loadedPackagePath,
            evidenceRoot, screenshots, plan.ExpectedObservation, backup, restored, normalBefore, Hash(normalConfig));
    }

    private async Task<SubscriberVerificationResult> VerifyPowerShellAsync(
        ReleaseProfile profile,
        string publishedFileId,
        SubscriberVerificationPlan plan,
        CancellationToken cancellationToken)
    {
        var normalConfig = ModListEditor.DefaultConfigPath();
        var normalBefore = Hash(normalConfig);
        var runId = NewRunId();
        var evidenceRoot = PrepareEvidenceRoot(profile.PackageId, runId, createDirectory: false);
        var executionScript = StagePowerShellAdapter(plan.Script!, runId);
        ProcessResult result;
        try
        {
            result = await ProcessRunner.RunAsync(
                "pwsh",
                [
                    "-NoProfile", "-NonInteractive", "-File", executionScript,
                    "-PublishedFileId", publishedFileId,
                    "-ExpectedInstallPath", plan.WorkshopPackagePath,
                    "-ArtifactsPath", evidenceRoot,
                    "-TimeoutSeconds", plan.TimeoutSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "-Output", "json"
                ],
                _repositoryRoot,
                TimeSpan.FromSeconds(plan.TimeoutSeconds + 120),
                cancellationToken);
        }
        finally
        {
            if (File.Exists(executionScript)) File.Delete(executionScript);
        }
        if (result.ExitCode != 0)
            throw new InvalidOperationException("Subscriber adapter failed: " + Bound(result.StandardError + result.StandardOutput));
        var json = result.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault()
                   ?? throw new InvalidOperationException("Subscriber adapter returned no result.");
        using var document = JsonDocument.Parse(json);
        var response = document.RootElement;
        if (GatewayWorkshopClient.String(response, "status") != "passed")
            throw new InvalidOperationException("Subscriber adapter did not report passed status.");
        var loadedPackagePath = GatewayWorkshopClient.String(response, "loadedPackagePath");
        RequireExactPath(loadedPackagePath, plan.WorkshopPackagePath);
        var screenshots = response.GetProperty("screenshots").EnumerateArray()
            .Select(item => Path.GetFullPath(item.GetString()!)).ToArray();
        if (screenshots.Length == 0 || screenshots.Any(path => !File.Exists(path)))
            throw new InvalidOperationException("Subscriber adapter did not retain its declared screenshots.");
        var restored = response.GetProperty("localProductRestored").GetBoolean();
        var normalAfter = Hash(normalConfig);
        if (!restored) throw new InvalidOperationException("Subscriber adapter did not restore the local product package.");
        if (!string.Equals(normalBefore, normalAfter, StringComparison.Ordinal))
            throw new InvalidOperationException("Subscriber adapter changed the user's normal ModsConfig.xml.");
        return new SubscriberVerificationResult(
            "passed", profile.PackageId, publishedFileId, plan.WorkshopPackagePath, loadedPackagePath,
            evidenceRoot, screenshots, plan.ExpectedObservation, null, true, normalBefore, normalAfter);
    }

    internal string PrepareEvidenceRoot(string packageId, string runId, bool createDirectory)
    {
        var parent = Path.Combine(_repositoryRoot, "artifacts", "Releases", packageId, "subscriber");
        Directory.CreateDirectory(parent);
        var path = Path.Combine(parent, runId);
        if (createDirectory) Directory.CreateDirectory(path);
        return path;
    }

    internal string StagePowerShellAdapter(string sourcePath, string runId)
    {
        var source = RepositoryRoot.ContainedPath(_repositoryRoot, sourcePath);
        var artifacts = Path.Combine(_repositoryRoot, "artifacts");
        Directory.CreateDirectory(artifacts);
        var destination = Path.Combine(artifacts, $"subscriber-adapter-{runId}.ps1");
        File.Copy(source, destination, overwrite: false);
        return destination;
    }

    private async Task RecoverInterruptedAsync(ReleaseProfile profile, CancellationToken cancellationToken)
    {
        var path = RecoveryPath(profile.PackageId);
        if (!File.Exists(path)) return;
        var recovery = JsonSerializer.Deserialize(
            File.ReadAllText(path), McpJsonContext.Default.SubscriberRecoveryRecord) ??
                       throw new InvalidOperationException("Subscriber recovery state is empty.");
        if (recovery.Schema != "RimWorldModdingMcp/SubscriberRecovery/v1" ||
            !string.Equals(recovery.PackageId, profile.PackageId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Subscriber recovery state does not match this package.");
        var modsRoot = Path.GetFullPath(@"F:\Steam\steamapps\common\RimWorld\Mods").TrimEnd(Path.DirectorySeparatorChar);
        var recoveryRoot = Path.GetFullPath(LocalModInstaller.RecoveryRoot(modsRoot)).TrimEnd(Path.DirectorySeparatorChar);
        var local = Path.GetFullPath(recovery.LocalPackagePath);
        var backup = Path.GetFullPath(recovery.BackupPath);
        var expectedLocal = Path.Combine(modsRoot, profile.PackageId);
        var expectedBackupPrefix = Path.Combine(recoveryRoot, "subscriber-" + profile.PackageId + "-");
        if (!string.Equals(local, expectedLocal, StringComparison.OrdinalIgnoreCase) ||
            !backup.StartsWith(expectedBackupPrefix, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetFullPath(recovery.NormalConfigPath),
                Path.GetFullPath(ModListEditor.DefaultConfigPath()), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Subscriber recovery paths escape their exact local roots.");
        try { _ = await new RunLeaseManager(_repositoryRoot).CancelAsync(recovery.GatewayRunId, cancellationToken); }
        catch (ArgumentException) { /* reserved run never started */ }
        if (Directory.Exists(local) && Directory.Exists(backup))
            throw new InvalidOperationException("Subscriber recovery found both local and backup packages; refusing overwrite.");
        if (!Directory.Exists(local) && Directory.Exists(backup))
            Directory.Move(backup, local);
        if (!Directory.Exists(local) || Directory.Exists(backup))
            throw new InvalidOperationException("Subscriber recovery could not restore the exact local product package.");
        if (!File.Exists(recovery.NormalConfigPath) ||
            !string.Equals(Hash(recovery.NormalConfigPath), recovery.NormalConfigSha256, StringComparison.Ordinal))
            throw new InvalidOperationException("Subscriber recovery found that the user's normal ModsConfig.xml changed.");
        File.Delete(path);
    }

    private string RecoveryPath(string packageId) => Path.Combine(
        _repositoryRoot, "artifacts", "Releases", packageId, "subscriber-active.json");

    private static void WriteRecovery(string path, SubscriberRecoveryRecord recovery) =>
        DurableFile.WriteAllText(path,
            JsonSerializer.Serialize(recovery, McpJsonContext.Default.SubscriberRecoveryRecord) + Environment.NewLine);

    private static string ContainedEvidencePath(string root, string relative)
    {
        var resolvedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var path = Path.GetFullPath(relative, resolvedRoot);
        if (!path.StartsWith(resolvedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Subscriber screenshot path escapes its evidence root.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
    }

    private static void RequireExactPath(string actual, string expected)
    {
        if (string.IsNullOrWhiteSpace(actual) ||
            !string.Equals(Path.GetFullPath(actual), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Subscriber workflow did not load the product from the exact Workshop item root.");
    }

    private static async Task<RunStatusResult> WaitReadyAsync(
        RunLeaseManager manager,
        string runId,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        while (DateTimeOffset.UtcNow < deadline)
        {
            var status = manager.Status(runId);
            if (status.State == "ready") return status;
            if (status.State is "exited" or "pid-reused")
                throw new InvalidOperationException("Subscriber Gateway run exited before readiness: " + status.StandardErrorTail);
            await Task.Delay(1000, cancellationToken);
        }
        throw new TimeoutException("Subscriber Gateway run did not become ready.");
    }

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static string NewRunId() => $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}";

    private static string Bound(string value) => value.Length <= 8192 ? value.Trim() : value[..8192].Trim();

    private static void RefuseRunningRimWorld()
    {
        var processes = System.Diagnostics.Process.GetProcessesByName("RimWorldWin64");
        try
        {
            if (processes.Length > 0) throw new InvalidOperationException("RimWorld is already running.");
        }
        finally
        {
            foreach (var process in processes) process.Dispose();
        }
    }
}
