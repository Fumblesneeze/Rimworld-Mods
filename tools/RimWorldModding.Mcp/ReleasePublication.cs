using System.Text.Json;

namespace RimWorldModding.Mcp;

public sealed record ReleaseAdmission(
    ReleasePublicationPlan Plan,
    string PlanPath,
    string PlanSha256,
    ReleaseCandidateStage Candidate);

public sealed record ReleasePlanStatusResult(
    string Status,
    string PackageId,
    string Title,
    string PlanPath,
    string PlanSha256,
    string CandidateDigest,
    DateTimeOffset ExpiresUtc,
    string DurableState,
    string? DurablePublishedFileId,
    string ReleaseWorkerState,
    int? ReleaseWorkerProcessId,
    bool AwaitingPersonalSubscriberReview,
    bool PersonallyReviewed);

public static class ReleasePlanAdmission
{
    private static readonly HashSet<string> RecoverableStages = new(StringComparer.Ordinal)
    {
        "create-admitted", "created", "submit-admitted", "submit-indeterminate", "submitted",
        "dependency-indeterminate", "steam-item-persisted", "steam-verified",
        "subscriber-evidence-awaiting-review", "complete-reviewed"
    };

    public static bool IsRecoverableDurableState(string value, string expectedPlanSha256, out string? publishedFileId)
    {
        publishedFileId = null;
        var parts = value.Trim().Split('|');
        if (parts.Length != 3 || !RecoverableStages.Contains(parts[0]) ||
            !string.Equals(parts[1], expectedPlanSha256, StringComparison.OrdinalIgnoreCase))
            return false;
        if (ulong.TryParse(parts[2], out var parsed) && parsed != 0)
            publishedFileId = parsed.ToString();
        return parts[0] == "create-admitted" ? parts[2] == "0" : publishedFileId is not null;
    }

    public static ReleasePlanStatusResult Status(
        string repositoryRoot,
        string planPath,
        string expectedPlanSha256,
        string confirmationNonce)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        var exactPath = RepositoryRoot.ContainedPath(root, planPath);
        if (!File.Exists(exactPath) || !IsHash(expectedPlanSha256) ||
            !string.Equals(ReleaseCandidateBuilder.Hash(exactPath), expectedPlanSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Publication status requires the exact retained plan SHA-256.");
        var plan = JsonSerializer.Deserialize(File.ReadAllText(exactPath), McpJsonContext.Default.ReleasePublicationPlan) ??
                   throw new InvalidOperationException("Publication plan is empty.");
        if (!string.Equals(plan.ConfirmationNonce, confirmationNonce, StringComparison.Ordinal) ||
            confirmationNonce.Length < 32 || confirmationNonce.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException("Admission nonce does not match the exact prepared plan.");
        var statePath = Path.Combine(
            root, "artifacts", "Releases", plan.PackageId, "publication-state.txt");
        var durableState = File.Exists(statePath) ? File.ReadAllText(statePath).Trim() : "none";
        var stateParts = durableState.Split('|');
        var matchingDurableState = stateParts.Length == 3 &&
                                   string.Equals(stateParts[1], expectedPlanSha256, StringComparison.OrdinalIgnoreCase);
        if (!matchingDurableState)
        {
            durableState = "none";
            stateParts = ["none"];
            _ = ValidateLocal(repositoryRoot, planPath, expectedPlanSha256, confirmationNonce,
                DateTimeOffset.UtcNow, requireCleanRevision: false);
        }
        var durableId = stateParts.Length == 3 && ulong.TryParse(stateParts[2], out var parsedId) && parsedId != 0
            ? parsedId.ToString()
            : null;
        var awaitingReview = stateParts.Length == 3 && stateParts[0] == "subscriber-evidence-awaiting-review";
        var reviewed = stateParts.Length == 3 && stateParts[0] == "complete-reviewed";
        var worker = new ReleaseWorkerCoordinator(root).Status(plan.PackageId, expectedPlanSha256);
        return new ReleasePlanStatusResult(
            reviewed ? "published-complete-personally-reviewed" :
            awaitingReview ? "published-awaiting-personal-subscriber-review" : "admitted-local-or-publication-in-progress",
            plan.PackageId,
            plan.Title,
            exactPath,
            expectedPlanSha256.ToUpperInvariant(),
            plan.CandidateDigest,
            plan.ExpiresUtc,
            durableState,
            durableId,
            worker.State,
            worker.ProcessId,
            awaitingReview,
            reviewed);
    }

    public static ReleaseAdmission ValidateLocal(
        string repositoryRoot,
        string planPath,
        string expectedPlanSha256,
        string confirmationNonce,
        DateTimeOffset now,
        bool requireCleanRevision)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        var path = RepositoryRoot.ContainedPath(root, planPath);
        if (!File.Exists(path)) throw new InvalidOperationException("Publication plan does not exist.");
        if (!IsHash(expectedPlanSha256) ||
            !string.Equals(ReleaseCandidateBuilder.Hash(path), expectedPlanSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Publication plan SHA-256 does not match the exact reviewed file.");
        ReleasePublicationPlan plan;
        try
        {
            plan = JsonSerializer.Deserialize(File.ReadAllText(path), McpJsonContext.Default.ReleasePublicationPlan) ??
                   throw new InvalidOperationException("Publication plan is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Publication plan is invalid JSON: {exception.Message}");
        }
        if (plan.Schema != "RimWorldModReleasePlan/v2" || plan.MutatesSteam)
            throw new InvalidOperationException("Publication plan schema or mutation-free dry-run marker is invalid.");
        ValidateFrozenProfile(root, plan);
        if (!string.Equals(plan.ConfirmationNonce, confirmationNonce, StringComparison.Ordinal) ||
            confirmationNonce.Length < 32 || confirmationNonce.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException("Admission nonce does not match the exact prepared plan.");
        if (plan.ExpiresUtc <= now) throw new InvalidOperationException("Publication plan admission has expired.");
        if (plan.ExpiresUtc > now.AddHours(3)) throw new InvalidOperationException("Publication plan expiry exceeds the bounded admission window.");
        if (plan.PublishedFileId is null && (!plan.AllowFirstPublication || plan.Visibility != "Private" || plan.ExactTitleMatches != 0))
            throw new InvalidOperationException("First publication is not an exact-title-absent Private opt-in.");
        if (plan.PublishedFileId is not null && plan.AllowFirstPublication)
            throw new InvalidOperationException("An existing item plan may not opt into first publication.");
        if (plan.PublishedFileId is null && plan.RemoteBaseline is not null)
            throw new InvalidOperationException("A first-publication plan may not carry an existing-item baseline.");
        if (plan.PublishedFileId is not null && plan.RemoteBaseline is null)
            throw new InvalidOperationException("An existing-item plan requires an exact authenticated remote baseline.");
        plan.RemoteBaseline?.AssertValid();
        if (string.IsNullOrWhiteSpace(plan.ChangeNote) || plan.ChangeNote.Trim().Length < 8)
            throw new InvalidOperationException("Publication plan changeNote is blank or non-specific.");
        if (!IsHash(plan.ReleaseProfileSha256) || !File.Exists(plan.ReleaseProfilePath) ||
            !string.Equals(ReleaseCandidateBuilder.Hash(plan.ReleaseProfilePath), plan.ReleaseProfileSha256, StringComparison.Ordinal))
            throw new InvalidOperationException("Release profile changed after preparation.");
        if (!IsHash(plan.DescriptionSha256) || !File.Exists(plan.DescriptionPath) ||
            !string.Equals(ReleaseCandidateBuilder.Hash(plan.DescriptionPath), plan.DescriptionSha256, StringComparison.Ordinal))
            throw new InvalidOperationException("Workshop description changed after preparation.");
        if (!IsHash(plan.PreviewSha256) || !File.Exists(plan.PreviewPath) ||
            !string.Equals(ReleaseCandidateBuilder.Hash(plan.PreviewPath), plan.PreviewSha256, StringComparison.Ordinal) ||
            new FileInfo(plan.PreviewPath).Length != plan.PreviewBytes)
            throw new InvalidOperationException("Workshop preview changed after preparation.");
        if (plan.VerificationFiles.Count == 0)
            throw new InvalidOperationException("Subscriber verification inputs are missing from the admitted plan.");
        foreach (var input in plan.VerificationFiles)
        {
            var inputPath = RepositoryRoot.ContainedPath(root, input.Path);
            if (!File.Exists(inputPath) || new FileInfo(inputPath).Length != input.Bytes || !IsHash(input.Sha256) ||
                !string.Equals(ReleaseCandidateBuilder.Hash(inputPath), input.Sha256, StringComparison.Ordinal))
                throw new InvalidOperationException("Subscriber verification input changed after preparation: " + input.Path);
        }
        var verificationRelative = Path.GetRelativePath(root, plan.VerificationProfile).Replace('\\', '/');
        if (!plan.VerificationFiles.Any(file => string.Equals(file.Path, verificationRelative, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Subscriber verification manifest is not bound into the admitted plan.");

        var includes = plan.Files.Select(file => file.Path).ToArray();
        var candidate = ReleaseCandidateBuilder.Inspect(plan.PackagePath, includes);
        if (!string.Equals(candidate.ContentDigest, plan.CandidateDigest, StringComparison.Ordinal) ||
            candidate.Files.Count != plan.Files.Count ||
            !candidate.Files.SequenceEqual(plan.Files))
            throw new InvalidOperationException("Release candidate files changed after preparation.");
        var allFiles = Directory.GetFiles(plan.PackagePath, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(plan.PackagePath, file).Replace('\\', '/'))
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToArray();
        if (!allFiles.SequenceEqual(plan.Files.Select(file => file.Path).OrderBy(file => file, StringComparer.Ordinal), StringComparer.Ordinal))
            throw new InvalidOperationException("Release candidate contains an undeclared file.");

        if (requireCleanRevision)
        {
            var status = ProcessRunner.RunAsync(
                    "git", ["status", "--porcelain", "--untracked-files=all"], root, TimeSpan.FromSeconds(30), CancellationToken.None)
                .GetAwaiter().GetResult();
            if (status.ExitCode != 0 || !string.IsNullOrWhiteSpace(status.StandardOutput))
                throw new InvalidOperationException("Steam publication requires the exact clean committed release worktree.");
            var revision = ProcessRunner.RunAsync(
                    "git", ["rev-parse", "HEAD"], root, TimeSpan.FromSeconds(30), CancellationToken.None)
                .GetAwaiter().GetResult();
            if (revision.ExitCode != 0 || !string.Equals(revision.StandardOutput.Trim(), plan.SourceRevision, StringComparison.Ordinal))
                throw new InvalidOperationException("Publication plan source revision is not the current committed revision.");
        }
        return new ReleaseAdmission(plan, path, expectedPlanSha256.ToUpperInvariant(), candidate);
    }

    public static ReleaseAdmission ValidateRecovery(
        string repositoryRoot,
        string planPath,
        string expectedPlanSha256,
        string confirmationNonce)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        var status = Status(root, planPath, expectedPlanSha256, confirmationNonce);
        if (!IsRecoverableDurableState(status.DurableState, expectedPlanSha256, out var durableId))
            throw new InvalidOperationException("Publication recovery requires an exact matching admitted durable state.");
        var path = RepositoryRoot.ContainedPath(root, planPath);
        var plan = JsonSerializer.Deserialize(File.ReadAllText(path), McpJsonContext.Default.ReleasePublicationPlan) ??
                   throw new InvalidOperationException("Publication plan is empty.");
        if (plan.Schema != "RimWorldModReleasePlan/v2" || plan.MutatesSteam ||
            !string.Equals(plan.ConfirmationNonce, confirmationNonce, StringComparison.Ordinal))
            throw new InvalidOperationException("Publication recovery plan schema or nonce is invalid.");
        ValidateFrozenProfile(root, plan);
        if (durableId is null)
            durableId = TryResolveConsistentIdentity(root, plan);

        RequireImmutableFile(plan.DescriptionPath, plan.DescriptionSha256, null, "Workshop description");
        RequireImmutableFile(plan.PreviewPath, plan.PreviewSha256, plan.PreviewBytes, "Workshop preview");
        foreach (var input in plan.VerificationFiles)
            RequireImmutableFile(RepositoryRoot.ContainedPath(root, input.Path), input.Sha256, input.Bytes,
                "Subscriber verification input");
        foreach (var file in plan.Files)
            RequireImmutableFile(Path.Combine(plan.PackagePath, file.Path.Replace('/', Path.DirectorySeparatorChar)),
                file.Sha256, file.Bytes, "Release candidate file");

        var allowed = plan.Files.Select(file => file.Path).ToHashSet(StringComparer.Ordinal);
        if (durableId is not null) allowed.Add("About/PublishedFileId.txt");
        var actual = Directory.GetFiles(plan.PackagePath, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(plan.PackagePath, file).Replace('\\', '/')).ToHashSet(StringComparer.Ordinal);
        if (!actual.SetEquals(allowed))
            throw new InvalidOperationException("Recovery candidate contains bytes outside the exact plan and durable Workshop identity.");
        if (durableId is not null)
        {
            var identity = Path.Combine(plan.PackagePath, "About", "PublishedFileId.txt");
            if (!File.Exists(identity) || File.ReadAllText(identity).Trim() != durableId)
                throw new InvalidOperationException("Recovery candidate Workshop identity does not match durable state.");
        }
        return new ReleaseAdmission(plan, path, expectedPlanSha256.ToUpperInvariant(),
            new ReleaseCandidateStage(plan.PackagePath, plan.CandidateDigest, plan.Files));
    }

    internal static string? TryResolveConsistentIdentity(string repositoryRoot, ReleasePublicationPlan plan)
    {
        var paths = new[]
        {
            Path.Combine(repositoryRoot, "artifacts", "Releases", plan.PackageId, "PublishedFileId.txt"),
            Path.Combine(Path.GetDirectoryName(plan.FrozenProfile.Project)!, "About", "PublishedFileId.txt"),
            Path.Combine(plan.PackagePath, "About", "PublishedFileId.txt")
        };
        if (paths.Any(path => !File.Exists(path))) return null;
        var values = paths.Select(path => File.ReadAllText(path).Trim()).ToArray();
        return values.Distinct(StringComparer.Ordinal).Count() == 1 &&
               ulong.TryParse(values[0], out var id) && id != 0 ? values[0] : null;
    }

    public static string AdmissionText(ReleaseAdmission admission) =>
        $"publish {admission.PlanSha256} {admission.Plan.ConfirmationNonce}";

    private static bool IsHash(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);

    private static void ValidateFrozenProfile(string root, ReleasePublicationPlan plan)
    {
        var frozenPath = RepositoryRoot.ContainedPath(root, plan.FrozenReleaseProfilePath);
        RequireImmutableFile(frozenPath, plan.ReleaseProfileSha256, null, "Frozen release profile");
        var frozenRelative = Path.GetRelativePath(root, frozenPath).Replace('\\', '/');
        if (!plan.VerificationFiles.Any(file =>
                string.Equals(file.Path, frozenRelative, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Frozen release profile is not bound into the admitted plan.");
        var profile = plan.FrozenProfile;
        if (!string.Equals(Path.GetFullPath(profile.Path), frozenPath, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(profile.PackageId, plan.PackageId, StringComparison.Ordinal) ||
            !string.Equals(profile.Title, plan.Title, StringComparison.Ordinal) ||
            !string.Equals(profile.Author, plan.Author, StringComparison.Ordinal) ||
            profile.SteamAppId != plan.SteamAppId ||
            !string.Equals(profile.SteamUserId, plan.SteamUserId, StringComparison.Ordinal) ||
            !string.Equals(profile.PublishedFileId, plan.PublishedFileId, StringComparison.Ordinal) ||
            profile.AllowFirstPublication != plan.AllowFirstPublication ||
            !string.Equals(profile.ChangeNote, plan.ChangeNote, StringComparison.Ordinal) ||
            !profile.Tags.SequenceEqual(plan.Tags, StringComparer.Ordinal) ||
            !profile.RequiredWorkshopItems.SequenceEqual(plan.RequiredWorkshopItems, StringComparer.Ordinal))
            throw new InvalidOperationException("Frozen release profile does not match the reviewed publication plan.");
        _ = RepositoryRoot.ContainedPath(root, profile.Project);
    }

    private static void RequireImmutableFile(string path, string expectedHash, long? expectedBytes, string label)
    {
        if (!File.Exists(path) || !IsHash(expectedHash) ||
            (expectedBytes.HasValue && new FileInfo(path).Length != expectedBytes.Value) ||
            !string.Equals(ReleaseCandidateBuilder.Hash(path), expectedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{label} changed after Steam mutation admission: {path}");
    }
}
