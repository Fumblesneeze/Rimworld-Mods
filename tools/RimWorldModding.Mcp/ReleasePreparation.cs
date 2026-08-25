using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace RimWorldModding.Mcp;

public sealed record WorkshopOwnerItem(string PublishedFileId, string Title);

public sealed record WorkshopOwnerPage(int Total, IReadOnlyList<WorkshopOwnerItem> Items);

public sealed record CandidateFile(string Path, long Bytes, string Sha256);

public sealed record ReleaseCandidateStage(string PackagePath, string ContentDigest, IReadOnlyList<CandidateFile> Files);

public sealed record RetainedPrivateReleaseEvidence(
    string PublicationPlanSha256,
    string PublishedFileId,
    string ChangeNote,
    CandidateFile WorkerResult,
    CandidateFile Receipt);

public sealed record PreviousPrivateReleaseEvidence(
    string PublicationPlanSha256,
    string PublishedFileId,
    string ChangeNote,
    CandidateFile WorkerResult,
    CandidateFile Receipt,
    CandidateFile FrozenWorkerResult,
    CandidateFile FrozenReceipt);

public sealed record ReleasePublicationPlan(
    string Schema,
    string PackageId,
    string Title,
    string Author,
    string SourceRevision,
    string CandidateDigest,
    string PackagePath,
    IReadOnlyList<CandidateFile> Files,
    string ReleaseProfilePath,
    string ReleaseProfileSha256,
    string FrozenReleaseProfilePath,
    ReleaseProfile FrozenProfile,
    string DescriptionPath,
    string DescriptionSha256,
    string PreviewPath,
    string PreviewSha256,
    long PreviewBytes,
    int SteamAppId,
    string SteamUserId,
    string? PublishedFileId,
    bool AllowFirstPublication,
    string Visibility,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> RequiredWorkshopItems,
    IReadOnlyList<string> RequiredDlcAppIds,
    string ChangeNote,
    string VerificationProfile,
    IReadOnlyList<CandidateFile> VerificationFiles,
    PreviousPrivateReleaseEvidence? PreviousPrivateReleaseEvidence,
    string OwnerScanUrl,
    int OwnerItemCount,
    IReadOnlyList<WorkshopOwnerItem> OwnerItems,
    int ExactTitleMatches,
    WorkshopRemoteBaseline? RemoteBaseline,
    IReadOnlyList<string> RemoteDiff,
    string ConfirmationNonce,
    DateTimeOffset ExpiresUtc,
    bool MutatesSteam);

public sealed record ReleasePreparationResult(
    string Status,
    string PlanPath,
    string PlanSha256,
    string CandidateDigest,
    string ConfirmationNonce,
    DateTimeOffset ExpiresUtc,
    string SourceRevision,
    string Title,
    string Visibility,
    IReadOnlyList<string> RequiredWorkshopItems,
    IReadOnlyList<string> RequiredDlcAppIds,
    IReadOnlyList<string> RemoteDiff,
    string PackagePath,
    string PreviewSha256);

public static class SteamWorkshopOwnerScanner
{
    private static readonly Regex Paging = new(
        @"Showing\s+\d+\s*-\s*\d+\s+of\s+(?<total>\d+)\s+entries",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex Item = new(
        @"data-publishedfileid=""(?<id>[1-9][0-9]{5,19})""[\s\S]{0,8192}?class=""workshopItemTitle[^""]*"">(?<title>[\s\S]*?)</div>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static WorkshopOwnerPage ParsePage(string html)
    {
        if (Encoding.UTF8.GetByteCount(html) > 4 * 1024 * 1024)
            throw new InvalidOperationException("Steam Workshop owner page exceeded 4 MiB.");
        var paging = Paging.Match(html);
        if (!paging.Success || !int.TryParse(paging.Groups["total"].Value, out var total))
            throw new InvalidOperationException("Steam Workshop owner page did not expose a bounded total.");
        var items = Item.Matches(html).Select(match => new WorkshopOwnerItem(
                match.Groups["id"].Value,
                WebUtility.HtmlDecode(Regex.Replace(match.Groups["title"].Value, "<[^>]+>", string.Empty)).Trim()))
            .DistinctBy(item => item.PublishedFileId, StringComparer.Ordinal)
            .ToArray();
        return new WorkshopOwnerPage(total, items);
    }

    public static async Task<(string Url, IReadOnlyList<WorkshopOwnerItem> Items)> ScanAsync(
        string steamUserId,
        int appId,
        CancellationToken cancellationToken)
    {
        if (!Regex.IsMatch(steamUserId, "^[1-9][0-9]{16,19}$"))
            throw new ArgumentException("Steam user ID is invalid.");
        var baseUrl =
            $"https://steamcommunity.com/profiles/{steamUserId}/myworkshopfiles/?appid={appId}&browsefilter=myfiles&view=imagewall&numperpage=30";
        using var http = new HttpClient(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All });
        http.DefaultRequestHeaders.UserAgent.ParseAdd("RimWorldModding.Mcp/0.1");
        var all = new Dictionary<string, WorkshopOwnerItem>(StringComparer.Ordinal);
        var expectedTotal = -1;
        for (var page = 1; page <= 100; page++)
        {
            var url = baseUrl + $"&p={page}";
            using var response = await http.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = ParsePage(html);
            if (expectedTotal < 0) expectedTotal = parsed.Total;
            if (parsed.Total != expectedTotal)
                throw new InvalidOperationException("Steam Workshop owner total changed during the bounded scan.");
            foreach (var item in parsed.Items) all[item.PublishedFileId] = item;
            if (all.Count >= expectedTotal) break;
            if (parsed.Items.Count == 0)
                throw new InvalidOperationException("Steam Workshop owner scan ended before returning every declared item.");
        }

        if (expectedTotal < 0 || all.Count != expectedTotal)
            throw new InvalidOperationException($"Steam Workshop owner scan returned {all.Count} of {expectedTotal} items.");
        return (baseUrl, all.Values.OrderBy(item => item.PublishedFileId, StringComparer.Ordinal).ToArray());
    }
}

public static class ReleaseCandidateBuilder
{
    public static ReleaseCandidateStage Inspect(string sourcePackage, IReadOnlyList<string> includes)
    {
        var source = Path.GetFullPath(sourcePackage).TrimEnd(Path.DirectorySeparatorChar);
        if (!Directory.Exists(source)) throw new ArgumentException($"Package source does not exist: {source}");
        var files = ResolveFiles(source, includes)
            .Select(path => new CandidateFile(
                Path.GetRelativePath(source, path).Replace('\\', '/'),
                new FileInfo(path).Length,
                Hash(path)))
            .OrderBy(file => file.Path, StringComparer.Ordinal)
            .ToArray();
        foreach (var file in files) RejectForbidden(file.Path);
        if (files.Length == 0) throw new InvalidOperationException("Release candidate contains zero files.");
        return new ReleaseCandidateStage(source, Digest(files), files);
    }

    public static ReleaseCandidateStage Stage(
        string sourcePackage,
        string destination,
        IReadOnlyList<string> includes)
    {
        var source = Path.GetFullPath(sourcePackage).TrimEnd(Path.DirectorySeparatorChar);
        var target = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar);
        if (!Directory.Exists(source)) throw new ArgumentException($"Package source does not exist: {source}");
        if (Directory.Exists(target) || File.Exists(target))
            throw new ArgumentException($"Candidate package destination already exists: {target}");
        Directory.CreateDirectory(target);
        try
        {
            foreach (var sourceFile in ResolveFiles(source, includes))
            {
                var relative = Path.GetRelativePath(source, sourceFile);
                RejectForbidden(relative);
                var output = Path.GetFullPath(Path.Combine(target, relative));
                AssertChild(target, output);
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                File.Copy(sourceFile, output, overwrite: false);
            }

            var files = Directory.GetFiles(target, "*", SearchOption.AllDirectories)
                .OrderBy(path => Path.GetRelativePath(target, path).Replace('\\', '/'), StringComparer.Ordinal)
                .Select(path => new CandidateFile(
                    Path.GetRelativePath(target, path).Replace('\\', '/'),
                    new FileInfo(path).Length,
                    Hash(path)))
                .ToArray();
            if (files.Length == 0) throw new InvalidOperationException("Release candidate contains zero files.");
            return new ReleaseCandidateStage(target, Digest(files), files);
        }
        catch
        {
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
            throw;
        }
    }

    private static IReadOnlyList<string> ResolveFiles(string source, IReadOnlyList<string> includes)
    {
        var files = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var include in includes)
        {
            if (Path.IsPathRooted(include) || include.Split('/', '\\').Any(part => part == ".."))
                throw new ArgumentException($"Package include escapes the source: {include}");
            var candidate = Path.GetFullPath(Path.Combine(source, include.Replace('/', Path.DirectorySeparatorChar)));
            AssertChild(source, candidate);
            foreach (var segment in PathsToInspect(source, candidate))
                RejectReparsePoint(segment, include);
            if (File.Exists(candidate))
            {
                files.Add(candidate);
            }
            else if (Directory.Exists(candidate))
            {
                foreach (var file in EnumerateFilesWithoutReparsePoints(candidate)) files.Add(file);
            }
            else
            {
                throw new ArgumentException($"Declared package include does not exist: {include}");
            }
        }
        return files.ToArray();
    }

    private static IEnumerable<string> EnumerateFilesWithoutReparsePoints(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            RejectReparsePoint(directory, directory);
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                RejectReparsePoint(file, file);
                yield return file;
            }
            foreach (var child in Directory.EnumerateDirectories(directory))
            {
                RejectReparsePoint(child, child);
                pending.Push(child);
            }
        }
    }

    private static void RejectReparsePoint(string path, string declared)
    {
        if (File.Exists(path) || Directory.Exists(path))
            RejectReparseAttributes(File.GetAttributes(path), declared);
    }

    internal static void RejectReparseAttributes(FileAttributes attributes, string declared)
    {
        if ((attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException($"Release package include crosses a reparse point: {declared}");
    }

    internal static IReadOnlyList<string> PathsToInspect(string root, string candidate)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
        var normalizedCandidate = Path.GetFullPath(candidate);
        AssertChild(normalizedRoot, normalizedCandidate);
        var relative = Path.GetRelativePath(normalizedRoot, normalizedCandidate);
        var paths = new List<string> { normalizedRoot };
        var current = normalizedRoot;
        foreach (var part in relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            paths.Add(current);
        }
        return paths;
    }

    private static void RejectForbidden(string relative)
    {
        var extension = Path.GetExtension(relative);
        if (extension.Equals(".pdb", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".trx", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Forbidden release file: {relative}");
        var name = Path.GetFileName(relative);
        if (name.Contains("RimWorldDevGateway", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("nunit", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("testhost", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Forbidden release file: {relative}");
    }

    private static void AssertChild(string root, string path)
    {
        var prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Resolved package path escapes its root: {path}");
    }

    public static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    public static string HashBytes(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static string Digest(IReadOnlyList<CandidateFile> files)
    {
        var canonical = string.Join(
            "\n",
            files.Select(file => $"{file.Path}\0{file.Bytes}\0{file.Sha256}")) + "\n";
        return HashBytes(Encoding.UTF8.GetBytes(canonical));
    }
}

public sealed class ReleasePreparer(string repositoryRoot)
{
    private readonly string _repositoryRoot = RepositoryRoot.Resolve(repositoryRoot);

    public async Task<ReleasePreparationResult> PrepareAsync(string packageId, CancellationToken cancellationToken)
    {
        var profile = ReleaseProfileCatalog.Discover(_repositoryRoot)
            .SingleOrDefault(item => string.Equals(item.PackageId, packageId, StringComparison.OrdinalIgnoreCase)) ??
            throw new ArgumentException($"No universal release profile exists for packageId '{packageId}'.");
        ReleaseEnvironmentValidator.Validate(profile);
        ReleaseChangeNotePolicy.Validate(profile);
        _ = SubscriberVerificationProfiles.Load(_repositoryRoot, profile.VerificationProfile);
        var initialPrivateHistory = await WorkshopChangeHistoryVerifier.ValidatePreviousAsync(
            _repositoryRoot, profile, cancellationToken);
        var revision = await RequireCleanRevisionAsync(cancellationToken);

        var build = await ProcessRunner.RunAsync(
            "dotnet",
            ["build", profile.Project, "-c", "Release"],
            _repositoryRoot,
            TimeSpan.FromMinutes(5),
            cancellationToken);
        if (build.ExitCode != 0)
            throw new InvalidOperationException($"Release build failed: {Bounded(build.StandardError + build.StandardOutput)}");

        await ValidatePresentationAsync(profile, cancellationToken);
        var afterRevision = await RequireCleanRevisionAsync(cancellationToken);
        if (!string.Equals(revision, afterRevision, StringComparison.Ordinal))
            throw new InvalidOperationException("Source revision changed during release preparation.");

        var runId = $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}"[..(24 + 8)];
        var runRoot = Path.Combine(_repositoryRoot, "artifacts", "Releases", profile.PackageId, runId);
        Directory.CreateDirectory(runRoot);
        var stage = ReleaseCandidateBuilder.Stage(
            profile.PackageSource,
            Path.Combine(runRoot, "package"),
            profile.PackageInclude);
        ReleasePackageValidator.Validate(profile, stage);
        IReadOnlyList<WorkshopOwnerItem> ownerItems = [];
        var exactMatches = 0;
        if (profile.PublishedFileId is null)
        {
            await ProveAuthenticatedTitleAbsenceAsync(profile, cancellationToken);
        }
        WorkshopRemoteBaseline? remoteBaseline = profile.PublishedFileId is null
            ? null
            : await InspectExistingRemoteAsync(profile, cancellationToken);
        if (remoteBaseline is not null)
            (ownerItems, exactMatches) = ProjectExistingOwnerEvidence(profile, remoteBaseline);
        var finalPrivateHistory = await WorkshopChangeHistoryVerifier.ValidatePreviousAsync(
            _repositoryRoot, profile, cancellationToken);
        if (initialPrivateHistory != finalPrivateHistory)
            throw new InvalidOperationException("Previous release evidence changed during release preparation.");

        var verificationRoot = Path.Combine(runRoot, "verification");
        Directory.CreateDirectory(verificationRoot);
        var frozenReleaseProfile = Path.Combine(verificationRoot, "release-profile.json");
        File.Copy(profile.Path, frozenReleaseProfile, overwrite: false);
        var canonicalManifest = SubscriberVerificationProfiles.Load(_repositoryRoot, profile.VerificationProfile);
        var frozenProfile = Path.Combine(verificationRoot, "subscriber-verification.json");
        var finalManifest = SubscriberVerificationProfiles.Freeze(
            _repositoryRoot, canonicalManifest, frozenProfile);
        var verificationFiles = SubscriberVerificationProfiles.CaptureInputs(
                _repositoryRoot, frozenProfile, finalManifest)
            .Append(new CandidateFile(
                Path.GetRelativePath(_repositoryRoot, frozenReleaseProfile).Replace('\\', '/'),
                new FileInfo(frozenReleaseProfile).Length,
                ReleaseCandidateBuilder.Hash(frozenReleaseProfile)))
            .ToList();
        PreviousPrivateReleaseEvidence? previousPrivateReleaseEvidence = null;
        if (finalPrivateHistory is not null)
        {
            var privateHistoryRoot = Path.Combine(verificationRoot, "previous-private-release");
            Directory.CreateDirectory(privateHistoryRoot);
            var frozenWorker = Path.Combine(privateHistoryRoot, "worker-result.json");
            var frozenReceipt = Path.Combine(privateHistoryRoot, "publication-receipt.json");
            File.Copy(
                RepositoryRoot.ContainedPath(_repositoryRoot, finalPrivateHistory.WorkerResult.Path),
                frozenWorker,
                overwrite: false);
            File.Copy(
                RepositoryRoot.ContainedPath(_repositoryRoot, finalPrivateHistory.Receipt.Path),
                frozenReceipt,
                overwrite: false);
            var frozenWorkerFile = CaptureFile(_repositoryRoot, frozenWorker);
            var frozenReceiptFile = CaptureFile(_repositoryRoot, frozenReceipt);
            if (finalPrivateHistory.WorkerResult.Bytes != frozenWorkerFile.Bytes ||
                !string.Equals(finalPrivateHistory.WorkerResult.Sha256, frozenWorkerFile.Sha256, StringComparison.OrdinalIgnoreCase) ||
                finalPrivateHistory.Receipt.Bytes != frozenReceiptFile.Bytes ||
                !string.Equals(finalPrivateHistory.Receipt.Sha256, frozenReceiptFile.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Previous Private release evidence changed while it was being frozen.");
            var afterFreezeHistory = WorkshopChangeHistoryVerifier.ValidateRetainedPrivateReceipt(
                _repositoryRoot, profile);
            if (finalPrivateHistory != afterFreezeHistory)
                throw new InvalidOperationException("Previous Private release evidence changed while the publication plan was being written.");
            verificationFiles.Add(frozenWorkerFile);
            verificationFiles.Add(frozenReceiptFile);
            previousPrivateReleaseEvidence = new PreviousPrivateReleaseEvidence(
                finalPrivateHistory.PublicationPlanSha256,
                finalPrivateHistory.PublishedFileId,
                finalPrivateHistory.ChangeNote,
                finalPrivateHistory.WorkerResult,
                finalPrivateHistory.Receipt,
                frozenWorkerFile,
                frozenReceiptFile);
        }
        var orderedVerificationFiles = verificationFiles
            .OrderBy(file => file.Path, StringComparer.Ordinal)
            .ToArray();
        var finalRevision = await RequireCleanRevisionAsync(cancellationToken);
        if (!string.Equals(revision, finalRevision, StringComparison.Ordinal))
            throw new InvalidOperationException("Source revision changed during remote release preflight.");

        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expires = DateTimeOffset.UtcNow.AddHours(2);
        var remoteDiff = profile.PublishedFileId is null
            ? new[]
            {
                $"CREATE Private Workshop item '{profile.Title}'",
                $"UPLOAD {stage.Files.Count} package files with digest {stage.ContentDigest}",
                $"SET primary preview SHA-256 {ReleaseCandidateBuilder.Hash(profile.Preview)}",
                $"SET required items [{string.Join(", ", profile.RequiredWorkshopItems)}]",
                $"SET required DLC applications [{string.Join(", ", profile.RequiredDlcAppIds)}]"
            }
            : WorkshopRemoteBaseline.DescribeDiff(remoteBaseline!, profile, stage);
        var plan = new ReleasePublicationPlan(
            "RimWorldModReleasePlan/v2",
            profile.PackageId,
            profile.Title,
            profile.Author,
            revision,
            stage.ContentDigest,
            stage.PackagePath,
            stage.Files,
            profile.Path,
            ReleaseCandidateBuilder.Hash(profile.Path),
            frozenReleaseProfile,
            profile with { Path = frozenReleaseProfile },
            profile.Description,
            ReleaseCandidateBuilder.Hash(profile.Description),
            profile.Preview,
            ReleaseCandidateBuilder.Hash(profile.Preview),
            new FileInfo(profile.Preview).Length,
            profile.SteamAppId,
            profile.SteamUserId,
            profile.PublishedFileId,
            profile.AllowFirstPublication,
            profile.PublishedFileId is null ? "Private" : profile.Visibility,
            profile.Tags,
            profile.RequiredWorkshopItems,
            profile.RequiredDlcAppIds,
            profile.ChangeNote,
            frozenProfile,
            orderedVerificationFiles,
            previousPrivateReleaseEvidence,
            $"steam-authenticated://owner/{profile.SteamUserId}/app/{profile.SteamAppId}",
            ownerItems.Count,
            ownerItems,
            exactMatches,
            remoteBaseline,
            remoteDiff,
            nonce,
            expires,
            false);
        var planPath = Path.Combine(runRoot, "publication-plan.json");
        var planJson = JsonSerializer.Serialize(plan, McpJsonContext.Default.ReleasePublicationPlan);
        DurableFile.WriteAllText(planPath, planJson + Environment.NewLine);
        var planHash = ReleaseCandidateBuilder.Hash(planPath);
        return new ReleasePreparationResult(
            "ready-for-publish",
            planPath,
            planHash,
            stage.ContentDigest,
            nonce,
            expires,
            revision,
            profile.Title,
            plan.Visibility,
            profile.RequiredWorkshopItems,
            profile.RequiredDlcAppIds,
            remoteDiff,
            stage.PackagePath,
            plan.PreviewSha256);
    }

    private static CandidateFile CaptureFile(string root, string path) =>
        new(
            Path.GetRelativePath(root, path).Replace('\\', '/'),
            new FileInfo(path).Length,
            ReleaseCandidateBuilder.Hash(path));

    internal static (IReadOnlyList<WorkshopOwnerItem> Items, int ExactTitleMatches) ProjectExistingOwnerEvidence(
        ReleaseProfile profile,
        WorkshopRemoteBaseline baseline)
    {
        baseline.AssertItemIdentity(profile);
        return (
            [new WorkshopOwnerItem(baseline.PublishedFileId, baseline.Title)],
            string.Equals(baseline.Title, profile.Title, StringComparison.Ordinal) ? 1 : 0);
    }

    private async Task<WorkshopRemoteBaseline> InspectExistingRemoteAsync(
        ReleaseProfile profile,
        CancellationToken cancellationToken)
    {
        var manager = new RunLeaseManager(_repositoryRoot);
        var start = manager.StartGateway(Array.Empty<string>(), Array.Empty<string>(), 900);
        try
        {
            RunStatusResult ready;
            var deadline = DateTimeOffset.UtcNow.AddMinutes(8);
            while (true)
            {
                ready = manager.Status(start.RunId);
                if (ready.State == "ready") break;
                if (ready.State is "exited" or "pid-reused")
                    throw new InvalidOperationException("Release preflight Gateway exited before readiness.");
                if (DateTimeOffset.UtcNow >= deadline)
                    throw new TimeoutException("Release preflight Gateway did not become ready.");
                await Task.Delay(1000, cancellationToken);
            }

            var client = new GatewayWorkshopClient(_repositoryRoot, ready.GatewayManifestPath!, ready.GameProcessId!.Value);
            await client.RegisterAsync(cancellationToken);
            var status = await client.InvokeAsync(new Dictionary<string, object?> { ["operation"] = "status" }, cancellationToken);
            WorkshopRemoteBaseline.AssertRuntimeIdentity(status, profile);
            var correlation = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            _ = await client.InvokeAsync(new Dictionary<string, object?>
            {
                ["operation"] = "query",
                ["planSha256"] = correlation,
                ["publishedFileId"] = profile.PublishedFileId
            }, cancellationToken);
            var remote = await client.WaitTerminalAsync(
                correlation,
                new HashSet<string>(["queried", "failed"], StringComparer.Ordinal),
                DateTimeOffset.UtcNow.AddMinutes(4),
                cancellationToken);
            if (GatewayWorkshopClient.String(remote, "Status") != "queried")
                throw new InvalidOperationException("Authenticated existing-item preflight query failed.");
            var baseline = await WorkshopRemoteBaseline.CaptureAsync(remote, cancellationToken);
            baseline.AssertItemIdentity(profile);
            return baseline;
        }
        finally
        {
            try { _ = await manager.CancelAsync(start.RunId, CancellationToken.None); }
            catch { /* exact run lease retains cleanup evidence */ }
        }
    }

    private async Task ProveAuthenticatedTitleAbsenceAsync(
        ReleaseProfile profile,
        CancellationToken cancellationToken)
    {
        var manager = new RunLeaseManager(_repositoryRoot);
        var start = manager.StartGateway(Array.Empty<string>(), Array.Empty<string>(), 900);
        try
        {
            RunStatusResult ready;
            var deadline = DateTimeOffset.UtcNow.AddMinutes(8);
            while (true)
            {
                ready = manager.Status(start.RunId);
                if (ready.State == "ready") break;
                if (ready.State is "exited" or "pid-reused")
                    throw new InvalidOperationException("Release title-preflight Gateway exited before readiness.");
                if (DateTimeOffset.UtcNow >= deadline)
                    throw new TimeoutException("Release title-preflight Gateway did not become ready.");
                await Task.Delay(1000, cancellationToken);
            }

            var client = new GatewayWorkshopClient(_repositoryRoot, ready.GatewayManifestPath!, ready.GameProcessId!.Value);
            await client.RegisterAsync(cancellationToken);
            var status = await client.InvokeAsync(new Dictionary<string, object?> { ["operation"] = "status" }, cancellationToken);
            WorkshopRemoteBaseline.AssertRuntimeIdentity(status, profile);
            var correlation = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            _ = await client.InvokeAsync(new Dictionary<string, object?>
            {
                ["operation"] = "owner-scan",
                ["planSha256"] = correlation,
                ["title"] = profile.Title
            }, cancellationToken);
            var scan = await client.WaitTerminalAsync(
                correlation,
                new HashSet<string>(["owner-scan-complete", "owner-scan-found", "failed"], StringComparer.Ordinal),
                DateTimeOffset.UtcNow.AddMinutes(4),
                cancellationToken);
            if (GatewayWorkshopClient.String(scan, "Status") != "owner-scan-complete")
                throw new InvalidOperationException("Authenticated Steam owner scan did not prove exact-title absence.");
        }
        finally
        {
            try { _ = await manager.CancelAsync(start.RunId, CancellationToken.None); }
            catch { /* exact run lease retains cleanup evidence */ }
        }
    }

    private async Task<string> RequireCleanRevisionAsync(CancellationToken cancellationToken)
    {
        var status = await ProcessRunner.RunAsync(
            "git", ["status", "--porcelain", "--untracked-files=all"], _repositoryRoot, TimeSpan.FromSeconds(30), cancellationToken);
        if (status.ExitCode != 0) throw new InvalidOperationException($"Could not inspect release worktree: {Bounded(status.StandardError)}");
        if (!string.IsNullOrWhiteSpace(status.StandardOutput))
            throw new InvalidOperationException("A publishable release requires a clean committed worktree.");
        var revision = await ProcessRunner.RunAsync(
            "git", ["rev-parse", "HEAD"], _repositoryRoot, TimeSpan.FromSeconds(30), cancellationToken);
        if (revision.ExitCode != 0) throw new InvalidOperationException($"Could not resolve release revision: {Bounded(revision.StandardError)}");
        return revision.StandardOutput.Trim();
    }

    private async Task ValidatePresentationAsync(ReleaseProfile profile, CancellationToken cancellationToken)
    {
        if (!File.Exists(profile.Preview) || new FileInfo(profile.Preview).Length is <= 0 or >= 1048576)
            throw new InvalidOperationException("Primary Workshop preview is missing, empty, or at least 1 MiB.");
        if (!File.Exists(profile.Description) || new FileInfo(profile.Description).Length == 0)
            throw new InvalidOperationException("Workshop description is missing or empty.");

        var manifest = Path.Combine(Path.GetDirectoryName(profile.Preview)!, "presentation.json");
        if (!File.Exists(manifest)) return;
        using var document = JsonDocument.Parse(File.ReadAllText(manifest));
        if (!document.RootElement.TryGetProperty("renderer", out var renderer) ||
            !renderer.TryGetProperty("path", out var rendererPath) ||
            rendererPath.ValueKind != JsonValueKind.String ||
            !renderer.TryGetProperty("sha256", out var rendererHash) ||
            rendererHash.ValueKind != JsonValueKind.String)
            return;
        var relative = rendererPath.GetString()!;
        var script = RepositoryRoot.ContainedPath(_repositoryRoot, relative);
        var scriptsRoot = Path.Combine(_repositoryRoot, "scripts") + Path.DirectorySeparatorChar;
        if (!script.StartsWith(scriptsRoot, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetExtension(script), ".ps1", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Presentation renderer is not a contained PowerShell migration adapter: {relative}");
        if (!File.Exists(script) ||
            !string.Equals(ReleaseCandidateBuilder.Hash(script), rendererHash.GetString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Presentation renderer does not match the hash pinned by its manifest.");
        var render = await ProcessRunner.RunAsync(
            "pwsh",
            ["-NoProfile", "-NonInteractive", "-File", script, "-ManifestPath", manifest, "-Output", "json"],
            _repositoryRoot,
            TimeSpan.FromMinutes(2),
            cancellationToken);
        if (render.ExitCode != 0)
            throw new InvalidOperationException($"Presentation render validation failed: {Bounded(render.StandardError + render.StandardOutput)}");
    }

    private static string Bounded(string value) => value.Length <= 8192 ? value.Trim() : value[..8192].Trim();
}
