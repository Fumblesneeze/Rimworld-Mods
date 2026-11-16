using NUnit.Framework;
using System.Text.Json;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class SplitWorkshopPublicationTests
{
    [TestCase("package", "Private", "Public", "Public")]
    [TestCase("package", "Public", "Private", "Private")]
    [TestCase("package", "Private", null, "Private")]
    [TestCase("page", "Public", "Private", "Public")]
    public void Package_history_uses_preserved_visibility(string action, string requested, string? current, string expected) =>
        Assert.That(WorkshopChangeHistoryVerifier.EffectiveVisibility(action, requested, current), Is.EqualTo(expected));

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void Private_package_history_survives_a_later_page_completion(bool migrate, bool precedingPackageWasPublic)
    {
        var root = Path.Combine(Path.GetTempPath(), "private-package-history-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "mods"));
        Directory.CreateDirectory(Path.Combine(root, "openspec"));
        File.WriteAllText(Path.Combine(root, "AGENTS.md"), "test fixture");
        try
        {
            var planPath = ReleasePublicationAdmissionTests.WritePlan(root, DateTimeOffset.UtcNow.AddMinutes(30), new string('A', 64), "dll");
            var plan = JsonSerializer.Deserialize<ReleasePublicationPlan>(File.ReadAllText(planPath))!;
            var profile = plan.FrozenProfile with { PublishedFileId = "123", PreviousChangeNote = "First tested package." };
            var packageHash = new string('B', 64);
            var releaseRoot = Path.Combine(root, "artifacts", "Releases", profile.PackageId);
            var receiptRoot = Path.Combine(releaseRoot, "publication", "package");
            var workerRoot = Path.Combine(releaseRoot, "workers", packageHash);
            Directory.CreateDirectory(receiptRoot);
            Directory.CreateDirectory(workerRoot);
            var receiptPath = Path.Combine(receiptRoot, "publication-receipt.json");
            File.WriteAllText(receiptPath, JsonSerializer.Serialize(new
            {
                schema = "RimWorldModReleaseReceipt/v1", publicationAction = "package", packageId = profile.PackageId,
                title = plan.Title, publicationPlanSha256 = packageHash, candidateDigest = plan.CandidateDigest,
                publishedFileId = "123", changeNote = profile.PreviousChangeNote,
                changeNoteVerification = new { status = precedingPackageWasPublic ? "latest-public-change-note-verified" : "steam-callback-confirmed-private-history-not-publicly-readable", changeNote = profile.PreviousChangeNote },
                remote = new { PublishedFileId = 123UL, RemoteUpdatedUnixSeconds = 101u },
                steamModifiedTime = new { BeforeUnixSeconds = 100u, AfterUnixSeconds = 101u, Advanced = true }
            }));
            var result = new ReleasePublishResult("published-complete-steam-verified", profile.PackageId, plan.Title,
                "123", "https://example.invalid", packageHash, plan.CandidateDigest, receiptPath,
                ReleaseCandidateBuilder.Hash(receiptPath), "commit", 100u, 101u, true);
            File.WriteAllText(Path.Combine(workerRoot, "worker-result.json"), JsonSerializer.Serialize(result, McpJsonContext.Default.ReleasePublishResult));
            File.WriteAllText(Path.Combine(releaseRoot, migrate ? "publication-state.txt" : "package-publication-state.txt"), $"complete-steam-verified|{packageHash}|123");
            if (migrate) ReleasePublisher.PreservePreviousPackageHistory(root, profile);
            File.WriteAllText(Path.Combine(releaseRoot, "publication-state.txt"), $"complete-steam-verified|{new string('C', 64)}|123");
            var retained = WorkshopChangeHistoryVerifier.ValidateRetainedPrivateReceipt(root, profile);
            Assert.That(retained.PublicationPlanSha256, Is.EqualTo(packageHash));
            Assert.That(retained.ChangeNote, Is.EqualTo(profile.PreviousChangeNote));
        }
        finally { Directory.Delete(root, true); }
    }

    [Test]
    public void Legacy_success_requires_its_exact_completed_receipt_before_a_new_plan()
    {
        var root = Path.Combine(Path.GetTempPath(), "legacy-publication-" + Guid.NewGuid().ToString("N"));
        var receiptRoot = Path.Combine(root, "publication", "old-run");
        Directory.CreateDirectory(receiptRoot);
        try
        {
            var state = Path.Combine(root, "publication-state.txt");
            var oldHash = new string('A', 64);
            var newHash = new string('B', 64);
            File.WriteAllText(state, $"succeeded|{oldHash}|123");
            Assert.That(() => ReleasePublisher.RefuseIndeterminateDuplicate(state, newHash, "123"), Throws.InvalidOperationException);
            File.WriteAllText(Path.Combine(receiptRoot, "publication-receipt.json"), JsonSerializer.Serialize(new
            {
                schema = "ImmersiveChefs/WorkshopPublicationReceipt/v1", publicationPlanSha256 = oldHash,
                publishedFileId = "123", remote = new { PublishedFileId = 123UL, RemoteMetadata = oldHash },
                subscribedSmoke = new { status = "passed", localProductRestored = true }, tokenRetained = false
            }));
            Assert.That(() => ReleasePublisher.RefuseIndeterminateDuplicate(state, newHash, "123"), Throws.Nothing);
            Assert.That(() => ReleasePublisher.RefuseIndeterminateDuplicate(state, newHash, "456"), Throws.InvalidOperationException);
            File.WriteAllText(state, $"submit-indeterminate|{oldHash}|123");
            Assert.That(() => ReleasePublisher.RefuseIndeterminateDuplicate(state, newHash, "123"), Throws.InvalidOperationException);
        }
        finally { Directory.Delete(root, true); }
    }

    [TestCase("package")]
    [TestCase("page")]
    public void Steam_request_contains_only_the_selected_actions_inputs(string action)
    {
        var root = Path.Combine(Path.GetTempPath(), "split-publication-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = ReleasePublicationAdmissionTests.WritePlan(root, DateTimeOffset.UtcNow.AddMinutes(30), new string('A', 64), "dll");
            var plan = JsonSerializer.Deserialize<ReleasePublicationPlan>(File.ReadAllText(path))! with
            {
                PublicationAction = action,
                PublishedFileId = "123",
                AdditionalPreviews = [new CandidateFile("preview.png", 7, new string('B', 64))],
                WorkshopLinks = []
            };
            var admission = new ReleaseAdmission(plan, path, new string('C', 64),
                new ReleaseCandidateStage(plan.PackagePath, plan.CandidateDigest, plan.Files));
            var request = ReleasePublisher.BuildPublishArguments(admission, 123, "id", "repo-id", "package-id", "state");
            Assert.Multiple(() =>
            {
                Assert.That(request["publicationAction"], Is.EqualTo(action));
                Assert.That(request.ContainsKey("packagePath"), Is.EqualTo(action == "package"));
                Assert.That(request.ContainsKey("descriptionPath"), Is.EqualTo(action == "page"));
                Assert.That(request.ContainsKey("previewPath"), Is.EqualTo(action == "page"));
                Assert.That(request.ContainsKey("additionalPreviewPaths"), Is.EqualTo(action == "page"));
                Assert.That(request["changeNote"], Is.EqualTo(action == "package" ? plan.ChangeNote : ""));
            });
        }
        finally { Directory.Delete(root, true); }
    }

    [TestCase("release_package_upload")]
    [TestCase("release_page_update")]
    public void Independent_actions_reject_unknown_packages_before_side_effects(string operation)
    {
        var root = FindRoot();
        var registry = OperationRegistry.CreateDefault(root);
        Assert.That(async () => await registry.InvokeAsync(operation,
                JsonSerializer.SerializeToElement(new { packageId = "fumblesneeze.does-not-exist" }),
                CancellationToken.None),
            Throws.ArgumentException.With.Message.Contains("No universal release profile"));
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RimWorldMods.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository not found.");
    }
}
