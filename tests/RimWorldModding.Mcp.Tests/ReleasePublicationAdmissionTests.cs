using System.Text.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class ReleasePublicationAdmissionTests
{
    [Test]
    public void Admission_BindsExactPlanDigestNonceExpiryAndCandidateFiles()
    {
        var root = TestRoot();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var nonce = new string('A', 64);
            var planPath = WritePlan(root, now.AddMinutes(30), nonce, "dll");
            var planHash = ReleaseCandidateBuilder.Hash(planPath);

            var admission = ReleasePlanAdmission.ValidateLocal(
                root, planPath, planHash, nonce, now, requireCleanRevision: false);
            Assert.That(admission.Plan.CandidateDigest, Is.EqualTo(admission.Candidate.ContentDigest));

            File.WriteAllText(Path.Combine(root, "candidate", "1.6", "Assemblies", "Product.dll"), "changed");
            Assert.Throws<InvalidOperationException>(() => ReleasePlanAdmission.ValidateLocal(
                root, planPath, planHash, nonce, now, requireCleanRevision: false));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Admission_RejectsWrongNonceAndExpiredPlan()
    {
        var root = TestRoot();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var nonce = new string('B', 64);
            var planPath = WritePlan(root, now.AddMinutes(-1), nonce, "dll");
            var hash = ReleaseCandidateBuilder.Hash(planPath);
            Assert.Throws<InvalidOperationException>(() => ReleasePlanAdmission.ValidateLocal(
                root, planPath, hash, new string('C', 64), now, requireCleanRevision: false));
            Assert.Throws<InvalidOperationException>(() => ReleasePlanAdmission.ValidateLocal(
                root, planPath, hash, nonce, now, requireCleanRevision: false));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Admission_RejectsChangedSubscriberManifestBytes()
    {
        var root = TestRoot();
        try
        {
            var nonce = new string('E', 64);
            var planPath = WritePlan(root, DateTimeOffset.UtcNow.AddMinutes(30), nonce, "dll");
            var hash = ReleaseCandidateBuilder.Hash(planPath);
            File.WriteAllText(Path.Combine(root, "subscriber.json"), "changed");

            Assert.That(
                Assert.Throws<InvalidOperationException>(() => ReleasePlanAdmission.ValidateLocal(
                    root, planPath, hash, nonce, DateTimeOffset.UtcNow, requireCleanRevision: false))!.Message,
                Does.Contain("Subscriber"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void RecoveryAdmission_AllowsExpiredExactPlanOnlyAfterDurableSameItemAdmission()
    {
        var root = TestRoot();
        try
        {
            var nonce = new string('8', 64);
            var planPath = WritePlan(root, DateTimeOffset.UtcNow.AddMinutes(-1), nonce, "dll");
            var hash = ReleaseCandidateBuilder.Hash(planPath);
            var releaseRoot = Path.Combine(root, "artifacts", "Releases", "fumblesneeze.example");
            Directory.CreateDirectory(releaseRoot);
            File.WriteAllText(Path.Combine(releaseRoot, "publication-state.txt"), $"submitted|{hash}|1234567890");
            var candidateIdentity = Path.Combine(root, "candidate", "About", "PublishedFileId.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(candidateIdentity)!);
            File.WriteAllText(candidateIdentity, "1234567890");

            var recovered = ReleasePlanAdmission.ValidateRecovery(root, planPath, hash, nonce);

            Assert.That(recovered.PlanSha256, Is.EqualTo(hash));
            Assert.That(recovered.Plan.ExpiresUtc, Is.LessThan(DateTimeOffset.UtcNow));
            File.WriteAllText(Path.Combine(releaseRoot, "publication-state.txt"), $"submit-failed-definite|{hash}|1234567890");
            Assert.Throws<InvalidOperationException>(() =>
                ReleasePlanAdmission.ValidateRecovery(root, planPath, hash, nonce));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void RecoveryPolicy_NeverContainsASecondSteamPublishDispatch()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "tools", "RimWorldModding.Mcp", "ReleasePublisher.cs"));

        Assert.That(Regex.Matches(source, "\\[\\\"operation\\\"\\]\\s*=\\s*\\\"publish\\\"").Count,
            Is.EqualTo(1), "Only initial exact-plan admission may dispatch Steam publication.");
        Assert.Multiple(() =>
        {
            Assert.That(ReleasePlanAdmission.IsRecoverableDurableState(
                "create-admitted|" + new string('A', 64) + "|0", new string('A', 64), out var noId), Is.True);
            Assert.That(noId, Is.Null);
            Assert.That(ReleasePlanAdmission.IsRecoverableDurableState(
                "submit-indeterminate|" + new string('A', 64) + "|123", new string('A', 64), out var sameId), Is.True);
            Assert.That(sameId, Is.EqualTo("123"));
            Assert.That(ReleasePlanAdmission.IsRecoverableDurableState(
                "create-failed-definite|" + new string('A', 64) + "|0", new string('A', 64), out _), Is.False);
        });
    }

    [Test]
    public void RecoveryAdmission_ResolvesFirstPublicationIdentityFromTheFrozenPlanWithoutCurrentProfileDiscovery()
    {
        var root = TestRoot();
        try
        {
            var nonce = new string('9', 64);
            var planPath = WritePlan(root, DateTimeOffset.UtcNow.AddMinutes(30), nonce, "dll");
            var hash = ReleaseCandidateBuilder.Hash(planPath);
            var plan = JsonSerializer.Deserialize(
                File.ReadAllText(planPath), McpJsonContext.Default.ReleasePublicationPlan)!;
            var releaseRoot = Path.Combine(root, "artifacts", "Releases", plan.PackageId);
            Directory.CreateDirectory(releaseRoot);
            File.WriteAllText(Path.Combine(releaseRoot, "publication-state.txt"), $"create-admitted|{hash}|0");
            File.WriteAllText(Path.Combine(releaseRoot, "PublishedFileId.txt"), "1234567890");
            var repositoryIdentity = Path.Combine(Path.GetDirectoryName(plan.FrozenProfile.Project)!, "About", "PublishedFileId.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(repositoryIdentity)!);
            File.WriteAllText(repositoryIdentity, "1234567890");
            var candidateIdentity = Path.Combine(plan.PackagePath, "About", "PublishedFileId.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(candidateIdentity)!);
            File.WriteAllText(candidateIdentity, "1234567890");

            var recovered = ReleasePlanAdmission.ValidateRecovery(root, planPath, hash, nonce);

            Assert.That(ReleasePlanAdmission.TryResolveConsistentIdentity(root, recovered.Plan),
                Is.EqualTo("1234567890"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void IdentityProjection_ReadsTheFrozenProfileRatherThanMutableCurrentBytes()
    {
        var root = TestRoot();
        try
        {
            var nonce = new string('6', 64);
            var planPath = WritePlan(root, DateTimeOffset.UtcNow.AddMinutes(30), nonce, "dll");
            var plan = JsonSerializer.Deserialize(
                File.ReadAllText(planPath), McpJsonContext.Default.ReleasePublicationPlan)!;
            File.WriteAllText(plan.ReleaseProfilePath, "changed after Steam admission");

            var projected = ReleasePublisher.ProjectFrozenProfileIdentity(plan, 1234567890);

            Assert.That(projected, Does.Contain("\"publishedFileId\": \"1234567890\""));
            Assert.That(projected, Does.Contain("\"allowFirstPublication\": false"));
            Assert.That(projected, Does.Contain("\"previousChangeNote\": \"Initial release.\""));
            Assert.That(projected, Does.Contain("\"changeNote\": \"\""));
            Assert.That(projected, Does.Contain("About/PublishedFileId.txt"));
            Assert.That(projected, Does.Not.Contain("changed after Steam admission"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void IdentityProjection_MatchesCanonicalProfileWithMixedWindowsAndUnixLineEndings()
    {
        var root = TestRoot();
        try
        {
            var planPath = WritePlan(root, DateTimeOffset.UtcNow.AddMinutes(30), new string('A', 64), "dll");
            var plan = JsonSerializer.Deserialize(
                File.ReadAllText(planPath), McpJsonContext.Default.ReleasePublicationPlan)!;
            var projected = ReleasePublisher.ProjectFrozenProfileIdentity(plan, 1234567890);
            var lines = projected.Replace("\r\n", "\n").Split('\n');
            var mixed = string.Join("", lines.Select((line, index) =>
                index == lines.Length - 1 ? line : line + (index % 2 == 0 ? "\r\n" : "\n")));
            File.WriteAllText(plan.ReleaseProfilePath, mixed);

            Assert.That(
                ReleasePublisher.CanonicalProfileMatchesProjectedIdentity(plan, 1234567890),
                Is.True);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void IdentityPersistenceRouting_SeparatesRecoveredFirstCommitFromExistingItemIdentity()
    {
        var root = TestRoot();
        try
        {
            var planPath = WritePlan(root, DateTimeOffset.UtcNow.AddMinutes(30), new string('5', 64), "dll");
            var plan = JsonSerializer.Deserialize(
                File.ReadAllText(planPath), McpJsonContext.Default.ReleasePublicationPlan)!;

            Assert.That(ReleasePublisher.IdentityPersistenceMode(plan, projectedIdentityIsClean: false),
                Is.EqualTo("persist-first-identity"));
            Assert.That(ReleasePublisher.IdentityPersistenceMode(plan, projectedIdentityIsClean: true),
                Is.EqualTo("reuse-first-identity-commit"));
            Assert.That(ReleasePublisher.IdentityPersistenceMode(
                    plan with { PublishedFileId = "1234567890", FrozenProfile = plan.FrozenProfile with
                    {
                        PublishedFileId = "1234567890", AllowFirstPublication = false
                    } },
                    projectedIdentityIsClean: false),
                Is.EqualTo("existing-identity"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void ExistingItemLocalInventory_DoesNotCountPublishedIdentityTwice()
    {
        var root = TestRoot();
        try
        {
            var planPath = WritePlan(root, DateTimeOffset.UtcNow.AddMinutes(30), new string('4', 64), "dll");
            var plan = JsonSerializer.Deserialize(
                File.ReadAllText(planPath), McpJsonContext.Default.ReleasePublicationPlan)!;
            var identity = new CandidateFile("About/PublishedFileId.txt", 10, new string('A', 64));

            Assert.That(ReleasePublisher.ExpectedLocalInventoryCount(plan), Is.EqualTo(plan.Files.Count + 1));
            Assert.That(ReleasePublisher.ExpectedLocalInventoryCount(plan with { Files = [.. plan.Files, identity] }),
                Is.EqualTo(plan.Files.Count + 1));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void IdentityCommitRecovery_ConsidersOnlyDirectChildrenOfTheAdmittedRevision()
    {
        const string source = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        const string direct = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";
        const string later = "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC";
        const string other = "DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD";
        var graph = $"{later} {direct}\n{direct} {source}\n{other} EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE\n{source}\n";

        Assert.That(ReleasePublisher.DirectChildCommitCandidates(graph, source), Is.EqualTo(new[] { direct }));
    }

    [Test]
    public void NewPlan_RefusesAnUnresolvedPriorUpdateButAllowsReviewedOrDefiniteFailure()
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"publication-state-{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllText(path, "submit-indeterminate|" + new string('A', 64) + "|123");
            Assert.That(() => ReleasePublisher.RefuseIndeterminateDuplicate(
                    path, new string('B', 64), "123"),
                Throws.InvalidOperationException.With.Message.Contains("previous Workshop update"));

            File.WriteAllText(path, "complete-reviewed|" + new string('A', 64) + "|123");
            Assert.DoesNotThrow(() => ReleasePublisher.RefuseIndeterminateDuplicate(
                path, new string('B', 64), "123"));
            File.WriteAllText(path, "submit-failed-definite|" + new string('A', 64) + "|123");
            Assert.DoesNotThrow(() => ReleasePublisher.RefuseIndeterminateDuplicate(
                path, new string('B', 64), "123"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public void Status_RecoversAnAdmittedPublicationAfterExpiryAndReviewCompletesExactEvidence()
    {
        var root = TestRoot();
        try
        {
            var nonce = new string('D', 64);
            var planPath = WritePlan(root, DateTimeOffset.UtcNow.AddMinutes(-1), nonce, "dll");
            var hash = ReleaseCandidateBuilder.Hash(planPath);
            var stateRoot = Path.Combine(root, "artifacts", "Releases", "fumblesneeze.example");
            Directory.CreateDirectory(stateRoot);
            File.WriteAllText(Path.Combine(stateRoot, "publication-state.txt"),
                $"subscriber-evidence-awaiting-review|{hash}|1234567890");
            var evidence = Path.Combine(stateRoot, "subscriber", "run");
            Directory.CreateDirectory(evidence);
            var before = Path.Combine(evidence, "before.png");
            var after = Path.Combine(evidence, "after.png");
            var png = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
            File.WriteAllBytes(before, png);
            File.WriteAllBytes(after, png);
            var receiptRoot = Path.Combine(stateRoot, "publication", "run");
            Directory.CreateDirectory(receiptRoot);
            var receipt = Path.Combine(receiptRoot, "publication-receipt.json");
            File.WriteAllText(receipt, JsonSerializer.Serialize(new
            {
                schema = "RimWorldModReleaseReceipt/v1",
                packageId = "fumblesneeze.example",
                title = "Example",
                publicationPlanSha256 = hash,
                candidateDigest = ReleasePlanAdmission.Status(root, planPath, hash, nonce).CandidateDigest,
                publishedFileId = "1234567890",
                subscriberEvidenceStatus = "awaiting-personal-review",
                subscriber = new { status = "passed", evidenceRoot = evidence, screenshots = new[] { before, after } }
            }));
            var workerRoot = Path.Combine(stateRoot, "workers", hash);
            Directory.CreateDirectory(workerRoot);
            var workerResult = new ReleasePublishResult(
                "published-steam-verified-subscriber-evidence-awaiting-personal-review",
                "fumblesneeze.example", "Example", "1234567890", "https://example.invalid", hash,
                ReleasePlanAdmission.Status(root, planPath, hash, nonce).CandidateDigest,
                receipt, ReleaseCandidateBuilder.Hash(receipt), evidence, "awaiting", "commit", "local", true, true);
            File.WriteAllText(Path.Combine(workerRoot, "worker-result.json"),
                JsonSerializer.Serialize(workerResult, McpJsonContext.Default.ReleasePublishResult));

            var status = ReleasePlanAdmission.Status(root, planPath, hash, nonce);
            Assert.That(status.AwaitingPersonalSubscriberReview, Is.True);
            Assert.That(status.DurablePublishedFileId, Is.EqualTo("1234567890"));

            var reviewed = ReleaseReview.Accept(root, planPath, hash, nonce, receipt,
                "I inspected the before and after frames and observed the native action change the result.");
            Assert.That(reviewed.Status, Is.EqualTo("complete-personally-reviewed"));
            Assert.That(reviewed.Screenshots, Has.Count.EqualTo(2));
            Assert.That(ReleasePlanAdmission.Status(root, planPath, hash, nonce).PersonallyReviewed, Is.True);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void DurableWorkerResult_RejectsIncompleteStateAndEmptyReceipt()
    {
        var root = TestRoot();
        try
        {
            var nonce = new string('F', 64);
            var planPath = WritePlan(root, DateTimeOffset.UtcNow.AddMinutes(-1), nonce, "dll");
            var hash = ReleaseCandidateBuilder.Hash(planPath);
            var releaseRoot = Path.Combine(root, "artifacts", "Releases", "fumblesneeze.example");
            Directory.CreateDirectory(releaseRoot);
            File.WriteAllText(Path.Combine(releaseRoot, "publication-state.txt"), $"submitted|{hash}|1234567890");
            var workerRoot = Path.Combine(releaseRoot, "workers", hash);
            Directory.CreateDirectory(workerRoot);
            var receiptRoot = Path.Combine(releaseRoot, "publication", "run");
            Directory.CreateDirectory(receiptRoot);
            var receipt = Path.Combine(receiptRoot, "publication-receipt.json");
            File.WriteAllText(receipt, "{}");
            var status = ReleasePlanAdmission.Status(root, planPath, hash, nonce);
            var expected = new ReleasePublishResult(
                "published-steam-verified-subscriber-evidence-awaiting-personal-review",
                "fumblesneeze.example", "Example", "1234567890", "https://example.invalid", hash,
                status.CandidateDigest, receipt, ReleaseCandidateBuilder.Hash(receipt), "evidence", "awaiting", "commit", "local", true, true);
            File.WriteAllText(Path.Combine(workerRoot, "worker-result.json"),
                JsonSerializer.Serialize(expected, McpJsonContext.Default.ReleasePublishResult));

            Assert.That(
                Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await new ReleaseWorkerCoordinator(root).PublishAsync(
                        planPath, hash, nonce, CancellationToken.None))!.Message,
                Does.Contain("final subscriber-evidence durable state"));
            Assert.That(ReleasePlanAdmission.Status(root, planPath, hash, nonce).ReleaseWorkerState,
                Is.EqualTo("invalid-result"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void DurableWorkerResult_RejectsFabricatedMismatchedResult()
    {
        var root = TestRoot();
        try
        {
            var nonce = new string('7', 64);
            var planPath = WritePlan(root, DateTimeOffset.UtcNow.AddMinutes(30), nonce, "dll");
            var hash = ReleaseCandidateBuilder.Hash(planPath);
            var releaseRoot = Path.Combine(root, "artifacts", "Releases", "fumblesneeze.example");
            Directory.CreateDirectory(releaseRoot);
            File.WriteAllText(Path.Combine(releaseRoot, "publication-state.txt"), $"submitted|{hash}|1234567890");
            var workerRoot = Path.Combine(releaseRoot, "workers", hash);
            Directory.CreateDirectory(workerRoot);
            var fabricated = new ReleasePublishResult(
                "published-steam-verified-subscriber-evidence-awaiting-personal-review",
                "fumblesneeze.example", "Example", "1234567890", "https://example.invalid", hash,
                "wrong-candidate", "outside.json", new string('A', 64), "evidence", "awaiting", "commit", "local", true, true);
            File.WriteAllText(Path.Combine(workerRoot, "worker-result.json"),
                JsonSerializer.Serialize(fabricated, McpJsonContext.Default.ReleasePublishResult));

            Assert.That(async () => await new ReleaseWorkerCoordinator(root).PublishAsync(
                    planPath, hash, nonce, CancellationToken.None),
                Throws.InvalidOperationException.With.Message.Contains("exact admitted plan"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string WritePlan(string root, DateTimeOffset expiry, string nonce, string dll)
    {
        var candidate = Path.Combine(root, "candidate");
        Directory.CreateDirectory(Path.Combine(candidate, "1.6", "Assemblies"));
        File.WriteAllText(Path.Combine(candidate, "1.6", "Assemblies", "Product.dll"), dll);
        var file = Path.Combine(candidate, "1.6", "Assemblies", "Product.dll");
        var files = new[] { new CandidateFile("1.6/Assemblies/Product.dll", new FileInfo(file).Length, ReleaseCandidateBuilder.Hash(file)) };
        var digest = ReleaseCandidateBuilder.Inspect(candidate, ["1.6/Assemblies/Product.dll"]).ContentDigest;
        var profile = Path.Combine(root, "profile.json");
        var project = Path.Combine(root, "mods", "Example", "Example.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(project)!);
        File.WriteAllText(project, "<Project />");
        var description = Path.Combine(root, "description.txt");
        var preview = Path.Combine(root, "preview.png");
        var profileJson = """
            {
              "schema": "RimWorldModRelease/v1",
              "publishedFileId": null,
              "allowFirstPublication": true,
              "packageInclude": [ "1.6/Assemblies/Product.dll" ]
            }
            """ + Environment.NewLine;
        File.WriteAllText(profile, profileJson);
        File.WriteAllText(description, "description");
        File.WriteAllText(preview, "preview");
        var subscriber = Path.Combine(root, "subscriber.json");
        File.WriteAllText(subscriber, "subscriber");
        var frozenProfile = Path.Combine(root, "frozen-release-profile.json");
        File.WriteAllText(frozenProfile, profileJson);
        var subscriberFile = new CandidateFile(
            Path.GetRelativePath(root, subscriber).Replace('\\', '/'),
            new FileInfo(subscriber).Length,
            ReleaseCandidateBuilder.Hash(subscriber));
        var frozenProfileFile = new CandidateFile(
            Path.GetRelativePath(root, frozenProfile).Replace('\\', '/'),
            new FileInfo(frozenProfile).Length,
            ReleaseCandidateBuilder.Hash(frozenProfile));
        var frozen = new ReleaseProfile(
            "RimWorldModRelease/v1", frozenProfile, project, "fumblesneeze.example", "Example", "Fumblesneeze",
            "Product", "1.6", "1.6.4871 rev590", "1.6.4871 rev591", "23969874", new string('A', 64),
            294100, "76561198077136238", null, true, "Private", ["Mod", "1.6"], ["2009463077"], [],
            "mod_build", "presentation_render", candidate, ["1.6/Assemblies/Product.dll"], description, preview,
            null, "Initial release.", subscriber);
        var plan = new ReleasePublicationPlan(
            "RimWorldModReleasePlan/v2", "fumblesneeze.example", "Example", "Fumblesneeze", "revision",
            digest, candidate, files, profile, ReleaseCandidateBuilder.Hash(profile), frozenProfile, frozen,
            description, ReleaseCandidateBuilder.Hash(description), preview, ReleaseCandidateBuilder.Hash(preview),
            new FileInfo(preview).Length, 294100, "76561198077136238", null, true, "Private", ["Mod", "1.6"],
            ["2009463077"], "Initial release.", subscriber, [subscriberFile, frozenProfileFile], "https://example.invalid", 0, [], 0,
            null, ["CREATE"], nonce, expiry, false);
        var path = Path.Combine(root, "publication-plan.json");
        File.WriteAllText(path, JsonSerializer.Serialize(plan));
        return path;
    }

    private static string TestRoot()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"publication-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "mods"));
        Directory.CreateDirectory(Path.Combine(root, "openspec"));
        File.WriteAllText(Path.Combine(root, "AGENTS.md"), "fixture");
        return root;
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AGENTS.md")) &&
                Directory.Exists(Path.Combine(current.FullName, "openspec"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("Repository root was not found.");
    }
}
