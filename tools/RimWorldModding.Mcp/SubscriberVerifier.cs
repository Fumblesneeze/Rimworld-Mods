using System.Security.Cryptography;
using System.Text;
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

internal sealed record PowerShellSubscriberEvidencePaths(
    string CanonicalRoot,
    string ExecutionRoot,
    string PromotionRoot);

public sealed class SubscriberVerifier(string repositoryRoot)
{
    private const int LegacyWindowsMaximumPathCharacters = 259;
    private const int PowerShellSubscriberNestedSuffixCharacters = 126;
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
        var runId = CreatePowerShellRunId(Guid.NewGuid());
        var evidencePaths = PreparePowerShellEvidencePaths(profile.PackageId, runId);
        var evidenceRoot = evidencePaths.CanonicalRoot;
        RequirePowerShellEvidencePathBudget(evidencePaths.ExecutionRoot);
        var executionScript = StagePowerShellAdapter(plan.Script!, runId);
        ProcessResult? result = null;
        Exception? processFailure = null;
        try
        {
            result = await ProcessRunner.RunAsync(
                "pwsh",
                [
                    "-NoProfile", "-NonInteractive", "-File", executionScript,
                    "-PublishedFileId", publishedFileId,
                    "-ExpectedInstallPath", plan.WorkshopPackagePath,
                    "-ArtifactsPath", evidencePaths.ExecutionRoot,
                    "-TimeoutSeconds", plan.TimeoutSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "-Output", "json"
                ],
                _repositoryRoot,
                TimeSpan.FromSeconds(plan.TimeoutSeconds + 120),
                cancellationToken);
        }
        catch (Exception exception)
        {
            processFailure = exception;
        }
        Exception? adapterCleanupFailure = null;
        try
        {
            if (File.Exists(executionScript)) File.Delete(executionScript);
        }
        catch (Exception exception)
        {
            adapterCleanupFailure = new InvalidOperationException(
                "The run-owned staged subscriber adapter could not be removed: " + executionScript, exception);
        }

        if (processFailure is not null)
        {
            Exception? retentionFailure = null;
            try
            {
                if (Directory.Exists(evidencePaths.ExecutionRoot)) PromotePowerShellEvidence(evidencePaths);
            }
            catch (Exception exception)
            {
                retentionFailure = exception;
            }
            throw CombineFailures(
                "Subscriber execution failed and its run-owned evidence or adapter cleanup also failed.",
                processFailure,
                retentionFailure,
                adapterCleanupFailure);
        }

        if (result is null) throw new InvalidOperationException("Subscriber process returned no result or failure.");
        if (result.ExitCode != 0)
        {
            var primary = new InvalidOperationException(
                "Subscriber adapter failed: " + Bound(result.StandardError + result.StandardOutput));
            Exception? retentionFailure = null;
            try
            {
                if (Directory.Exists(evidencePaths.ExecutionRoot)) PromotePowerShellEvidence(evidencePaths);
            }
            catch (Exception exception)
            {
                retentionFailure = exception;
            }
            throw CombineFailures(
                "Subscriber adapter failed and its run-owned evidence or adapter cleanup also failed at " +
                evidencePaths.ExecutionRoot + ".",
                primary,
                retentionFailure,
                adapterCleanupFailure);
        }
        if (!Directory.Exists(evidencePaths.ExecutionRoot))
            throw CombineFailures(
                "Subscriber adapter did not create its run-owned evidence root.",
                new InvalidOperationException("Subscriber adapter did not create its run-owned evidence root."),
                adapterCleanupFailure);
        try
        {
            PromotePowerShellEvidence(evidencePaths);
        }
        catch (Exception promotionFailure)
        {
            throw CombineFailures(
                "Subscriber evidence promotion or adapter cleanup failed.",
                promotionFailure,
                adapterCleanupFailure);
        }
        if (adapterCleanupFailure is not null) throw adapterCleanupFailure;
        var json = result.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault()
                   ?? throw new InvalidOperationException("Subscriber adapter returned no result.");
        using var document = JsonDocument.Parse(json);
        var response = document.RootElement;
        if (GatewayWorkshopClient.String(response, "status") != "passed")
            throw new InvalidOperationException("Subscriber adapter did not report passed status.");
        var loadedPackagePath = GatewayWorkshopClient.String(response, "loadedPackagePath");
        RequireExactPath(loadedPackagePath, plan.WorkshopPackagePath);
        var screenshots = response.GetProperty("screenshots").EnumerateArray()
            .Select(item => RebasePowerShellEvidencePath(evidencePaths, item.GetString()!)).ToArray();
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

    internal PowerShellSubscriberEvidencePaths PreparePowerShellEvidencePaths(string packageId, string runId)
    {
        var canonical = PrepareEvidenceRoot(packageId, runId, createDirectory: false);
        if (Directory.Exists(canonical) || File.Exists(canonical))
            throw new InvalidOperationException("Canonical subscriber evidence root already exists.");
        var promotion = canonical + ".incoming";
        if (Directory.Exists(promotion) || File.Exists(promotion))
            throw new InvalidOperationException("Canonical subscriber evidence promotion root already exists.");
        var temporaryParent = Path.Combine(Path.GetTempPath(), "rwsub");
        Directory.CreateDirectory(temporaryParent);
        var repositoryIdentity = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(_repositoryRoot)))
            .ToLowerInvariant()[..8];
        var execution = Path.Combine(temporaryParent, $"{repositoryIdentity}-{runId}");
        if (Directory.Exists(execution) || File.Exists(execution))
            throw new InvalidOperationException("Subscriber execution evidence path already exists.");
        RequirePowerShellEvidencePathBudget(execution);
        return new PowerShellSubscriberEvidencePaths(canonical, execution, promotion);
    }

    internal void PromotePowerShellEvidence(PowerShellSubscriberEvidencePaths paths)
    {
        if (!Directory.Exists(paths.ExecutionRoot))
            throw new InvalidOperationException("Subscriber execution evidence root does not exist.");
        if (Directory.Exists(paths.CanonicalRoot) || File.Exists(paths.CanonicalRoot))
            throw new InvalidOperationException("Canonical subscriber evidence root already exists.");
        if (Directory.Exists(paths.PromotionRoot) || File.Exists(paths.PromotionRoot))
            throw new InvalidOperationException("Canonical subscriber evidence promotion root already exists.");

        try
        {
            CopyAndVerifyEvidenceTree(paths.ExecutionRoot, paths.PromotionRoot);
            Directory.Move(paths.PromotionRoot, paths.CanonicalRoot);
        }
        catch (Exception promotionFailure)
        {
            try
            {
                if (Directory.Exists(paths.PromotionRoot)) Directory.Delete(paths.PromotionRoot, recursive: true);
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException(
                    "Subscriber evidence promotion failed and its run-owned partial copy could not be removed.",
                    promotionFailure,
                    cleanupFailure);
            }
            throw;
        }

        try { Directory.Delete(paths.ExecutionRoot, recursive: true); }
        catch { /* canonical verified evidence is authoritative; never mutate or reject it for duplicate cleanup */ }
    }

    internal string RebasePowerShellEvidencePath(PowerShellSubscriberEvidencePaths paths, string executionPath)
    {
        var executionRoot = Path.GetFullPath(paths.ExecutionRoot).TrimEnd(Path.DirectorySeparatorChar);
        var source = Path.GetFullPath(executionPath);
        var prefix = executionRoot + Path.DirectorySeparatorChar;
        if (!source.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Subscriber screenshot escapes its run-owned execution evidence root.");
        var relative = Path.GetRelativePath(executionRoot, source);
        if (relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(part => part == ".."))
            throw new InvalidOperationException("Subscriber screenshot escapes its run-owned execution evidence root.");
        return Path.Combine(paths.CanonicalRoot, relative);
    }

    private static void CopyAndVerifyEvidenceTree(string sourceRoot, string destinationRoot)
    {
        Directory.CreateDirectory(destinationRoot);
        var sourceInventory = InventoryEvidenceTree(sourceRoot);
        var sourceDirectories = sourceInventory.Directories;
        foreach (var sourceDirectory in sourceDirectories)
        {
            Directory.CreateDirectory(Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, sourceDirectory)));
        }

        var sourceFiles = sourceInventory.Files;
        foreach (var sourceFile in sourceFiles)
        {
            var destinationFile = Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, sourceFile));
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            File.Copy(sourceFile, destinationFile, overwrite: false);
            if (new FileInfo(sourceFile).Length != new FileInfo(destinationFile).Length ||
                !string.Equals(Hash(sourceFile), Hash(destinationFile), StringComparison.Ordinal))
                throw new InvalidOperationException("Subscriber evidence copy verification failed.");
        }

        var destinationInventory = InventoryEvidenceTree(destinationRoot);
        var destinationDirectories = destinationInventory.Directories
            .Select(path => Path.GetRelativePath(destinationRoot, path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var expectedDirectories = sourceDirectories
            .Select(path => Path.GetRelativePath(sourceRoot, path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var destinationFiles = destinationInventory.Files
            .Select(path => Path.GetRelativePath(destinationRoot, path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var expectedFiles = sourceFiles
            .Select(path => Path.GetRelativePath(sourceRoot, path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (!destinationDirectories.SequenceEqual(expectedDirectories, StringComparer.OrdinalIgnoreCase) ||
            !destinationFiles.SequenceEqual(expectedFiles, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Subscriber evidence tree copy is incomplete.");
    }

    private static (string[] Directories, string[] Files) InventoryEvidenceTree(string root)
    {
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("Subscriber evidence root is a reparse point.");
        var directories = new List<string>();
        var files = new List<string>();
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var entry in Directory.GetFileSystemEntries(directory, "*", SearchOption.TopDirectoryOnly))
            {
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Subscriber evidence contains a reparse point.");
                if ((attributes & FileAttributes.Directory) != 0)
                {
                    directories.Add(entry);
                    pending.Push(entry);
                }
                else
                {
                    files.Add(entry);
                }
            }
        }
        directories.Sort(StringComparer.OrdinalIgnoreCase);
        files.Sort(StringComparer.OrdinalIgnoreCase);
        return (directories.ToArray(), files.ToArray());
    }

    private static Exception CombineFailures(string message, Exception primary, params Exception?[] secondary)
    {
        var failures = new[] { primary }.Concat(secondary.OfType<Exception>()).ToArray();
        return failures.Length == 1 ? primary : new AggregateException(message, failures);
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

    internal static string CreatePowerShellRunId(Guid entropy) =>
        entropy.ToString("N")[..12];

    internal static void RequirePowerShellEvidencePathBudget(string evidenceRoot)
    {
        // Two 19-character timestamps, smoke-001, SavedData/DevGateway/Sessions,
        // one 32-character Gateway run ID, separators, and session.json total 126.
        if (evidenceRoot.Length + PowerShellSubscriberNestedSuffixCharacters >
            LegacyWindowsMaximumPathCharacters)
        {
            throw new InvalidOperationException(
                "Subscriber evidence root leaves insufficient room for the nested legacy Windows path.");
        }
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
