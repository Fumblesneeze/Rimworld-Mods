using System.Text.Json;
using System.Xml.Linq;

namespace RimWorldModding.Mcp;

internal static class ReleaseMatrixBuild
{
    internal static string ValidateTestProject(string root, string projectPath, string packageId)
    {
        var path = RepositoryRoot.ContainedPath(root, projectPath);
        if (!path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
            throw new ArgumentException("A repository-contained E2E project is required.");
        var project = XDocument.Load(path);
        var owners = project.Descendants("RimWorldEndToEndTestOwnerPackageId").Select(node => node.Value.Trim()).ToArray();
        if (!owners.SequenceEqual([packageId]) ||
            !project.Descendants("RimWorldPreparedProductReference").Select(node => node.Value.Trim()).SequenceEqual(["true"]))
            throw new ArgumentException("Native release tests require the requested product's E2E owner and prepared-reference opt-in.");
        return path;
    }

    internal static (string ReceiptPath, ReleaseCandidateStage Candidate) ReadLatest(string root, ReleaseProfile profile,
        string? requiredRevision = null)
    {
        var matrixRoot = Path.Combine(root, "artifacts", "Releases", profile.PackageId, "matrix") + Path.DirectorySeparatorChar;
        var pointer = Path.Combine(root, "artifacts", "Releases", profile.PackageId, "matrix-build-latest.txt");
        var receiptPath = RepositoryRoot.ContainedPath(root, File.ReadAllText(pointer).Trim());
        if (!receiptPath.StartsWith(matrixRoot, StringComparison.OrdinalIgnoreCase) || new FileInfo(receiptPath).Length > 8 * 1024 * 1024)
            throw new InvalidOperationException("Matrix receipt must be a bounded artifact of this product.");
        using var document = JsonDocument.Parse(File.ReadAllText(receiptPath));
        var receipt = document.RootElement;
        var candidatePath = RepositoryRoot.ContainedPath(root, receipt.GetProperty("candidate").GetProperty("packagePath").GetString()!);
        if (!candidatePath.StartsWith(Path.GetDirectoryName(receiptPath)! + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Matrix candidate escaped its build artifact.");
        var candidate = ReleaseCandidateBuilder.Inspect(candidatePath, profile.PackageInclude);
        ValidateReceipt(receipt, profile, root, candidate.ContentDigest, requiredRevision);
        ReleasePackageValidator.Validate(profile, candidate);
        return (receiptPath, candidate);
    }

    internal static void ValidateReceipt(JsonElement receipt, ReleaseProfile profile, string root, string digest, string? requiredRevision)
    {
        if (receipt.GetProperty("schema").GetString() != "RimWorldMatrixBuild/v1" ||
            receipt.GetProperty("configuration").GetString() != "Release" ||
            receipt.GetProperty("profileSha256").GetString() != ReleaseCandidateBuilder.Hash(profile.Path) ||
            receipt.GetProperty("catalogSha256").GetString() != ReleaseCandidateBuilder.Hash(Path.Combine(root, "release", "rimworld-targets.json")) ||
            receipt.GetProperty("candidate").GetProperty("contentDigest").GetString() != digest ||
            (requiredRevision is not null && (receipt.GetProperty("dirty").GetBoolean() || receipt.GetProperty("sourceRevision").GetString() != requiredRevision)))
            throw new InvalidOperationException("Matrix receipt no longer matches the clean source, Release configuration, profile, catalog or package. Rebuild the matrix.");
        var targets = receipt.GetProperty("targets").EnumerateArray()
            .Select(item => item.GetProperty("target").Deserialize<SteamGameTarget>(SteamGameTargets.Json)!).ToArray();
        if (JsonSerializer.Serialize(targets, SteamGameTargets.Json) != JsonSerializer.Serialize(profile.Targets, SteamGameTargets.Json))
            throw new InvalidOperationException("Matrix receipt targets differ from the declared matrix.");
    }

    internal static async Task<AdapterOperationResult> TestTargetAsync(string root, ReleaseProfile profile, string targetId,
        string cacheRoot, string projectPath, IReadOnlyList<string> testIds, int timeoutSeconds, bool dryRun, CancellationToken cancellationToken)
    {
        projectPath = ValidateTestProject(root, projectPath, profile.PackageId);
        var target = profile.Targets.SingleOrDefault(item => item.Id == targetId) ??
            throw new ArgumentException("Test target must belong to the product's declared matrix.");
        var game = SteamGameTargets.Validate(root, target, cacheRoot);
        var prepared = ReadLatest(root, profile);
        var assembly = prepared.Candidate.Files.Single(file => file.Path.StartsWith(target.Version + "/Assemblies/", StringComparison.Ordinal) && file.Path.EndsWith(".dll", StringComparison.Ordinal));
        var preflight = RepositoryOperationPlanner.EndToEnd(root, null, null, "English", timeoutSeconds, true,
            projectPath: projectPath, testIds: testIds, gameRoot: game.Root, preparedProductPackageId: profile.PackageId,
            preparedProductAssemblyPath: Path.Combine(prepared.Candidate.PackagePath, assembly.Path));
        var discovery = await EndToEndRunRecovery.ExecuteAsync(preflight, cancellationToken);
        if (dryRun || discovery.ExitCode != 0) return discovery;
        var command = RepositoryOperationPlanner.EndToEnd(root, null, null, "English", timeoutSeconds, dryRun,
            projectPath: projectPath, testIds: testIds, gameRoot: game.Root, preparedProductPackageId: profile.PackageId,
            preparedProductAssemblyPath: Path.Combine(prepared.Candidate.PackagePath, assembly.Path));
        if (!dryRun)
        {
            Directory.CreateDirectory(Path.Combine(game.Root, "Mods"));
            _ = LocalModInstaller.Sync(prepared.Candidate.PackagePath, Path.Combine(game.Root, "Mods"), profile.PackageId, profile.PackageInclude);
        }
        var result = await EndToEndRunRecovery.ExecuteAsync(command, cancellationToken);
        if (!dryRun)
        {
            var observed = ReleaseCandidateBuilder.Inspect(Path.Combine(game.Root, "Mods", profile.PackageId), profile.PackageInclude);
            if (observed.ContentDigest != prepared.Candidate.ContentDigest)
                throw new InvalidOperationException("Native verification changed the prepared product package.");
            File.Copy(prepared.ReceiptPath, Path.Combine(command.EvidenceRoot!, "matrix-build.json"));
            DurableFile.WriteAllText(Path.Combine(command.EvidenceRoot!, "matrix-target.json"), JsonSerializer.Serialize(new
            { target, game, prepared.Candidate.ContentDigest, result.ExitCode }, SteamGameTargets.Json));
        }
        return result;
    }

    internal static AdapterCommand PlanTarget(string repositoryRoot, ReleaseProfile profile, SteamGameTarget target,
        GameInstallation installation, string runRoot, string configuration)
    {
        if (configuration is not ("Release" or "Debug")) throw new ArgumentException("Configuration must be Release or Debug.");
        if (installation.MajorVersion != target.Version)
            throw new InvalidOperationException("Compiler inputs do not match the declared target.");
        var targetRoot = RepositoryRoot.ContainedPath(repositoryRoot, Path.Combine(runRoot, "targets", target.Id));
        var intermediate = Path.Combine(targetRoot, "obj") + Path.DirectorySeparatorChar;
        return new AdapterCommand("release_matrix_build", "dotnet",
            ["build", profile.Project, "-c", configuration, "--no-incremental",
             "-p:RimWorldPath=" + installation.Root, "-p:DefaultRimWorldPath=" + installation.Root,
             "-p:RimWorldManagedPath=" + installation.ManagedPath, "-p:RimWorldVersion=" + target.Version,
             "-p:BaseIntermediateOutputPath=" + intermediate, "-p:MSBuildProjectExtensionsPath=" + intermediate,
             "-p:OutputFolder=" + Path.Combine(targetRoot, "Mods"),
             "-p:RimWorldModPackageFolder=" + Path.Combine(targetRoot, "local", profile.PackageId),
             "-p:DeployToGame=false"], repositoryRoot, TimeSpan.FromMinutes(5), targetRoot);
    }

    internal static async Task<AdapterOperationResult> BuildAsync(string repositoryRoot, ReleaseProfile profile,
        string cacheRoot, string modsRoot, string configuration, CancellationToken cancellationToken)
    {
        if (profile.Targets.Count == 0) throw new InvalidOperationException("Profile has no declared release matrix.");
        var installations = profile.Targets.Select(target => SteamGameTargets.Validate(repositoryRoot, target, cacheRoot)).ToArray();
        var revision = await ProcessRunner.RunAsync("git", ["rev-parse", "HEAD"], repositoryRoot, TimeSpan.FromSeconds(15), cancellationToken);
        var dirty = await ProcessRunner.RunAsync("git", ["status", "--porcelain", "--untracked-files=all"], repositoryRoot, TimeSpan.FromSeconds(15), cancellationToken);
        if (revision.ExitCode != 0 || dirty.ExitCode != 0) throw new InvalidOperationException("Cannot bind matrix to source revision.");
        var runRoot = Path.Combine(repositoryRoot, "artifacts", "Releases", profile.PackageId, "matrix",
            DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(runRoot);
        var receipts = new List<object>();
        var combined = Path.Combine(runRoot, "package", profile.PackageId);
        Directory.CreateDirectory(combined);
        for (var index = 0; index < profile.Targets.Count; index++)
        {
            var target = profile.Targets[index];
            var command = PlanTarget(repositoryRoot, profile, target, installations[index], runRoot, configuration);
            Directory.CreateDirectory(command.EvidenceRoot!);
            var result = await ProcessRunner.RunAsync(command.FileName, command.Arguments, repositoryRoot, command.Timeout, cancellationToken);
            var log = Path.Combine(command.EvidenceRoot!, "build.log");
            DurableFile.WriteAllText(log, result.StandardOutput + result.StandardError);
            if (result.ExitCode != 0)
                throw new InvalidOperationException($"Target {target.Id} build failed; combined package was not installed. See {log}");
            var source = Path.Combine(command.EvidenceRoot!, "Mods", profile.PackageId);
            var includes = profile.PackageInclude.Where(path => path.StartsWith("About/", StringComparison.Ordinal) ||
                path.StartsWith(target.Version + "/", StringComparison.Ordinal)).ToArray();
            var candidate = ReleaseCandidateBuilder.Inspect(source, includes);
            ReleasePackageValidator.Validate(profile with { Targets = [], RimWorldVersion = target.Version }, candidate);
            foreach (var file in candidate.Files)
            {
                if (index > 0 && file.Path.StartsWith("About/", StringComparison.Ordinal)) continue;
                var destination = Path.Combine(combined, file.Path.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(Path.Combine(source, file.Path.Replace('/', Path.DirectorySeparatorChar)), destination, overwrite: false);
            }
            receipts.Add(new { target, installation = installations[index], command.Arguments, log, logSha256 = ReleaseCandidateBuilder.Hash(log),
                candidate.ContentDigest, candidate.Files, xmlProjection = "canonical-no-overrides" });
        }
        var aboutPath = Path.Combine(combined, "About", "About.xml");
        var about = XDocument.Load(aboutPath);
        about.Root!.Element("supportedVersions")!.ReplaceWith(new XElement("supportedVersions",
            profile.SupportedVersions.Select(version => new XElement("li", version))));
        about.Save(aboutPath);
        var final = ReleaseCandidateBuilder.Inspect(combined, profile.PackageInclude);
        ReleasePackageValidator.Validate(profile, final);
        var finalRevision = await ProcessRunner.RunAsync("git", ["rev-parse", "HEAD"], repositoryRoot, TimeSpan.FromSeconds(15), cancellationToken);
        var finalDirty = await ProcessRunner.RunAsync("git", ["status", "--porcelain", "--untracked-files=all"], repositoryRoot, TimeSpan.FromSeconds(15), cancellationToken);
        if (finalRevision.ExitCode != 0 || finalDirty.ExitCode != 0 || finalRevision.StandardOutput != revision.StandardOutput || finalDirty.StandardOutput != dirty.StandardOutput)
            throw new InvalidOperationException("Source changed during the matrix build; no combined package was installed.");
        Directory.CreateDirectory(Path.GetDirectoryName(profile.PackageSource)!);
        _ = LocalModInstaller.Sync(combined, Path.GetDirectoryName(profile.PackageSource)!, profile.PackageId, profile.PackageInclude);
        var installationResult = LocalModInstaller.Sync(profile.PackageSource, modsRoot, profile.PackageId, profile.PackageInclude);
        var receipt = new { schema = "RimWorldMatrixBuild/v1", configuration, sourceRevision = revision.StandardOutput.Trim(),
            dirty = !string.IsNullOrWhiteSpace(dirty.StandardOutput), profileSha256 = ReleaseCandidateBuilder.Hash(profile.Path),
            catalogSha256 = ReleaseCandidateBuilder.Hash(Path.Combine(repositoryRoot, "release", "rimworld-targets.json")),
            targets = receipts, candidate = final, installation = installationResult };
        var json = JsonSerializer.Serialize(receipt, SteamGameTargets.Json);
        var receiptPath = Path.Combine(runRoot, "matrix-build.json");
        DurableFile.WriteAllText(receiptPath, json);
        DurableFile.WriteAllText(Path.Combine(repositoryRoot, "artifacts", "Releases", profile.PackageId, "matrix-build-latest.txt"), receiptPath);
        return new AdapterOperationResult("release_matrix_build", 0, 0, json, "", runRoot);
    }
}
