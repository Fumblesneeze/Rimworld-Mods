using NUnit.Framework;
using System.Text.Json;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class ReleaseProfileTests
{
    private const string RepositoryUrl = "https://github.com/Fumblesneeze/Rimworld-Mods";

    [Test]
    public void Release_profiles_and_plans_do_not_select_a_product_specific_verification_workflow()
    {
        var root = TestRepository.FindRoot();

        Assert.Multiple(() =>
        {
            Assert.That(typeof(ReleaseProfile).GetProperty("VerificationProfile"), Is.Null);
            Assert.That(typeof(ReleasePublicationPlan).GetProperty("VerificationProfile"), Is.Null);
            Assert.That(
                File.ReadAllText(Path.Combine(root, "mods", "GuestBedGizmo", "Release", "release.json")),
                Does.Not.Contain("verificationProfile"));
            Assert.That(
                File.ReadAllText(Path.Combine(root, "mods", "ImmersiveChefs", "Release", "release.json")),
                Does.Not.Contain("verificationProfile"));
        });
    }

    [Test]
    public void Repository_profiles_do_not_claim_an_unsupported_GitHub_Workshop_link()
    {
        var root = TestRepository.FindRoot();

        foreach (var profile in ReleaseProfileCatalog.Discover(root).Where(profile => profile.PublishedFileId is not null))
        {
            Assert.That(
                profile.WorkshopLinks,
                Is.Empty,
                profile.PackageId + " must not claim a custom link Steam's player-facing editor cannot represent.");
            Assert.That(File.ReadAllText(profile.Description), Does.Not.Contain(RepositoryUrl),
                "Steam has no GitHub/custom Links field, and the author did not approve a description fallback.");
        }
    }

    [Test]
    public void Workshop_links_use_the_exact_lowercase_publisher_wire_shape()
    {
        Assert.That(
            JsonSerializer.Serialize(new[] { new WorkshopLink("github", RepositoryUrl) }),
            Is.EqualTo("[{\"key\":\"github\",\"url\":\"https://github.com/Fumblesneeze/Rimworld-Mods\"}]"));
    }

    [Test]
    public void Workshop_links_are_structurally_bounded_and_Steam_player_facing_keys_are_exact()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => WorkshopLinkPolicy.Validate([
                    new WorkshopLink("github", RepositoryUrl),
                    new WorkshopLink("GitHub", RepositoryUrl)]),
                Throws.TypeOf<ReleaseProfileException>().With.Message.Contains("unique"));
            Assert.That(
                () => WorkshopLinkPolicy.Validate([new WorkshopLink("github", "http://github.com/Fumblesneeze/Rimworld-Mods")]),
                Throws.TypeOf<ReleaseProfileException>().With.Message.Contains("HTTPS"));
            Assert.That(WorkshopLinkPolicy.Validate([]), Is.Empty);
            Assert.DoesNotThrow(() => WorkshopLinkPolicy.ValidateSteamPlayerFacingSupport([
                new WorkshopLink("facebook", "https://facebook.com/example"),
                new WorkshopLink("twitter", "https://x.com/example"),
                new WorkshopLink("youtube", "https://youtube.com/@example"),
                new WorkshopLink("polycount", "https://polycount.com/example"),
                new WorkshopLink("reddit", "https://reddit.com/r/example"),
                new WorkshopLink("sketchfab", "https://sketchfab.com/example")
            ]));
            Assert.That(
                () => WorkshopLinkPolicy.ValidateSteamPlayerFacingSupport([
                    new WorkshopLink("github", RepositoryUrl)]),
                Throws.TypeOf<ReleaseProfileException>()
                    .With.Message.Contains("does not expose a GitHub or custom player-facing Workshop link field"));
        });
    }

    [Test]
    public void ResolvedWorkshopDescriptions_FitSteamsExactUtf8Boundary()
    {
        var root = TestRepository.FindRoot();
        var profiles = ReleaseProfileCatalog.Discover(root);

        foreach (var profile in profiles)
        {
            Assert.DoesNotThrow(
                () => ReleasePresentationPolicy.ValidateDescription(profile.Description),
                profile.PackageId);
        }

        Assert.DoesNotThrow(() => ReleasePresentationPolicy.ValidateDescriptionBytes(
            Enumerable.Repeat((byte)'a', 7_999).ToArray()));
        Assert.That(
            Assert.Throws<InvalidOperationException>(
                () => ReleasePresentationPolicy.ValidateDescriptionBytes(
                    Enumerable.Repeat((byte)'a', 8_000).ToArray()))!.Message,
            Does.Contain("8,000"));
        Assert.That(
            Assert.Throws<InvalidOperationException>(
                () => ReleasePresentationPolicy.ValidateDescriptionBytes(new byte[] { 65, 0, 66 }))!.Message,
            Does.Contain("NUL"));
    }

    [Test]
    public void RepositoryProfiles_AreUniversalAndIdentityConsistent()
    {
        var root = TestRepository.FindRoot();
        var profiles = ReleaseProfileCatalog.Discover(root);

        Assert.That(profiles.Select(profile => profile.PackageId), Does.Contain("fumblesneeze.immersivechefs"));
        Assert.That(profiles.Select(profile => profile.PackageId), Does.Contain("fumblesneeze.guestbedgizmo"));
        Assert.That(profiles.Select(profile => profile.Schema).Distinct(), Is.EqualTo(new[] { "RimWorldModRelease/v1" }));
        Assert.That(profiles.Select(profile => profile.PackageId).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(profiles.Count));

        var guest = profiles.Single(profile => profile.PackageId == "fumblesneeze.guestbedgizmo");
        Assert.That(guest.Title, Is.EqualTo("Hospitality + Ideology Patch"));
        Assert.That(guest.PublishedFileId, Does.Match("^[1-9][0-9]{5,19}$"));
        Assert.That(guest.AllowFirstPublication, Is.False);
        Assert.That(
            File.ReadAllText(Path.Combine(root, "mods", "GuestBedGizmo", "About", "PublishedFileId.txt")).Trim(),
            Is.EqualTo(guest.PublishedFileId));
        Assert.That(guest.Visibility, Is.EqualTo("Private"));
        Assert.That(guest.RequiredWorkshopItems, Is.EquivalentTo(new[] { "2009463077", "3509486825" }));
        var requiredDlc = typeof(ReleaseProfile).GetProperty("RequiredDlcAppIds");
        Assert.That(requiredDlc, Is.Not.Null);
        Assert.That((IReadOnlyList<string>)requiredDlc!.GetValue(guest)!, Is.EqualTo(new[] { "1392840" }));
        Assert.That(typeof(ReleasePublicationPlan).GetProperty("RequiredDlcAppIds"), Is.Not.Null);
        Assert.That(typeof(ReleasePublicationPlan).GetProperty("PreviousPrivateReleaseEvidence"), Is.Not.Null);
        Assert.That(typeof(ReleasePreparationResult).GetProperty("RequiredDlcAppIds"), Is.Not.Null);
        Assert.DoesNotThrow(() => ReleaseEnvironmentValidator.Validate(guest));
        Assert.DoesNotThrow(() => ReleaseChangeNotePolicy.Validate(guest));
    }

    [Test]
    public void SteamDlcApplicationGraph_IsStrictAndSeparateFromWorkshopItems()
    {
        var root = TestRepository.FindRoot();
        var source = Path.Combine(root, "mods", "GuestBedGizmo", "Release", "release.json");
        var original = File.ReadAllText(source);
        var missingDlcField = Path.Combine(Path.GetDirectoryName(source)!, $"release-{Guid.NewGuid():N}.json");
        var duplicateDlc = Path.Combine(Path.GetDirectoryName(source)!, $"release-{Guid.NewGuid():N}.json");
        var consumerAppAsDlc = Path.Combine(Path.GetDirectoryName(source)!, $"release-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(missingDlcField, System.Text.RegularExpressions.Regex.Replace(
                original,
                "\\s*\"requiredDlcAppIds\"\\s*:\\s*\\[[^\\]]*\\]\\s*,",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.Singleline));
            File.WriteAllText(duplicateDlc, System.Text.RegularExpressions.Regex.Replace(
                original,
                "(\"requiredDlcAppIds\"\\s*:\\s*\\[\\s*\"1392840\")",
                "$1, \"1392840\"",
                System.Text.RegularExpressions.RegexOptions.Singleline));
            File.WriteAllText(consumerAppAsDlc, original.Replace("\"1392840\"", "\"294100\""));

            Assert.That(
                Assert.Throws<ReleaseProfileException>(() => ReleaseProfileCatalog.Load(root, missingDlcField))!.Message,
                Does.Contain("requiredDlcAppIds"));
            Assert.That(
                Assert.Throws<ReleaseProfileException>(() => ReleaseProfileCatalog.Load(root, duplicateDlc))!.Message,
                Does.Contain("unique"));
            Assert.That(
                Assert.Throws<ReleaseProfileException>(() => ReleaseProfileCatalog.Load(root, consumerAppAsDlc))!.Message,
                Does.Contain("consumer application"));
        }
        finally
        {
            File.Delete(missingDlcField);
            File.Delete(duplicateDlc);
            File.Delete(consumerAppAsDlc);
        }
    }

    [Test]
    public void UpdateChangeNotes_MustBeSpecificAndDifferentFromPinnedHistory()
    {
        var root = TestRepository.FindRoot();
        var guest = ReleaseProfileCatalog.Discover(root)
            .Single(profile => profile.PackageId == "fumblesneeze.guestbedgizmo");
        var generic = guest with
        {
            PublishedFileId = "1234567890", AllowFirstPublication = false,
            PreviousChangeNote = "A prior specific release note.", ChangeNote = "update"
        };
        var unchanged = generic with { ChangeNote = generic.PreviousChangeNote! };

        Assert.Throws<InvalidOperationException>(() => ReleaseChangeNotePolicy.Validate(generic));
        Assert.Throws<InvalidOperationException>(() => ReleaseChangeNotePolicy.Validate(unchanged));
    }

    [Test]
    public void ExistingItemOwnerEvidence_UsesTheAuthenticatedOldTitleDuringATitleCorrection()
    {
        var root = TestRepository.FindRoot();
        var guest = ReleaseProfileCatalog.Discover(root)
            .Single(profile => profile.PackageId == "fumblesneeze.guestbedgizmo");
        var baseline = WorkshopRemoteBaseline.Create(
            guest.PublishedFileId!,
            "Hospitality + Ideoligy Patch",
            new string('A', 64),
            100,
            guest.Tags,
            "prior-plan",
            "Private",
            "https://example.invalid/preview.png",
            new string('B', 64),
            guest.SteamUserId,
            guest.SteamAppId,
            100,
            1,
            guest.RequiredWorkshopItems,
            [],
            []);

        var projected = ReleasePreparer.ProjectExistingOwnerEvidence(guest, baseline);

        Assert.That(projected.Items.Single().Title, Is.EqualTo("Hospitality + Ideoligy Patch"));
        Assert.That(projected.ExactTitleMatches, Is.Zero);
    }

    [Test]
    public void ExistingPrivateItem_RequiresTheExactRetainedReviewedPublisherReceipt()
    {
        var root = TestRepository.FindRoot();
        var guest = ReleaseProfileCatalog.Discover(root)
            .Single(profile => profile.PackageId == "fumblesneeze.guestbedgizmo") with
        {
            PublishedFileId = "1234567890",
            AllowFirstPublication = false,
            PreviousChangeNote = "Initial specific release note."
        };

        var fixture = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"private-history-{Guid.NewGuid():N}");
        var packageRoot = Path.Combine(fixture, "artifacts", "Releases", guest.PackageId);
        var plan = new string('A', 64);
        var receipt = Path.Combine(packageRoot, "publication", "prior", "publication-receipt.json");
        var worker = Path.Combine(packageRoot, "workers", plan, "worker-result.json");
        var evidenceRoot = Path.Combine(packageRoot, "subscriber", "prior");
        var before = Path.Combine(evidenceRoot, "before.png");
        var after = Path.Combine(evidenceRoot, "after.png");
        const string title = "Prior private title";
        var candidateDigest = new string('B', 64);
        Directory.CreateDirectory(Path.Combine(fixture, "mods"));
        Directory.CreateDirectory(Path.Combine(fixture, "openspec"));
        File.WriteAllText(Path.Combine(fixture, "AGENTS.md"), "fixture");
        Directory.CreateDirectory(Path.GetDirectoryName(receipt)!);
        Directory.CreateDirectory(Path.GetDirectoryName(worker)!);
        Directory.CreateDirectory(evidenceRoot);
        File.WriteAllBytes(before, [1]);
        File.WriteAllBytes(after, [2]);
        File.WriteAllText(Path.Combine(packageRoot, "publication-state.txt"),
            $"complete-reviewed|{plan}|{guest.PublishedFileId}");
        File.WriteAllText(receipt, System.Text.Json.JsonSerializer.Serialize(new
        {
            schema = "RimWorldModReleaseReceipt/v1",
            packageId = guest.PackageId,
            title,
            publishedFileId = guest.PublishedFileId,
            publicationPlanSha256 = plan,
            candidateDigest,
            changeNote = guest.PreviousChangeNote,
            subscriberEvidenceStatus = "awaiting-personal-review",
            subscriber = new
            {
                status = "passed",
                evidenceRoot,
                screenshots = new[] { before, after }
            },
            changeNoteVerification = new
            {
                status = "steam-callback-confirmed-private-history-not-publicly-readable",
                changeNote = guest.PreviousChangeNote
            }
        }));
        File.WriteAllText(worker, System.Text.Json.JsonSerializer.Serialize(new
        {
            status = "published-steam-verified-subscriber-evidence-awaiting-personal-review",
            packageId = guest.PackageId,
            title,
            publishedFileId = guest.PublishedFileId,
            workshopUrl = $"https://steamcommunity.com/sharedfiles/filedetails/?id={guest.PublishedFileId}",
            publicationPlanSha256 = plan,
            candidateDigest,
            receiptPath = receipt,
            receiptSha256 = ReleaseCandidateBuilder.Hash(receipt),
            subscriberEvidenceRoot = evidenceRoot,
            subscriberEvidenceStatus = "awaiting personal review",
            identityCommit = "prior-identity-commit",
            localPackagePath = Path.Combine(fixture, "Mods", guest.PackageId),
            localPackageRestored = true,
            steamVerified = true
        }));
        try
        {
            var evidence = WorkshopChangeHistoryVerifier.ValidateRetainedPrivateReceipt(fixture, guest);
            Assert.That(evidence.WorkerResult.Sha256, Is.EqualTo(ReleaseCandidateBuilder.Hash(worker)));
            Assert.That(evidence.Receipt.Sha256, Is.EqualTo(ReleaseCandidateBuilder.Hash(receipt)));

            File.WriteAllText(Path.Combine(packageRoot, "publication-state.txt"),
                $"subscriber-evidence-awaiting-review|{plan}|{guest.PublishedFileId}");
            Assert.Throws<InvalidOperationException>(() =>
                WorkshopChangeHistoryVerifier.ValidateRetainedPrivateReceipt(fixture, guest));
        }
        finally
        {
            Directory.Delete(fixture, recursive: true);
        }
    }

    [Test]
    public void ChangeHistoryParser_RequiresTheNewestEntryRatherThanAnyOldNote()
    {
        const string html = """
            <div class="detailBox changeLogCtn"><p id="2">Newest player-facing note.</p></div>
            <div class="detailBox changeLogCtn"><p id="1">Older note.</p></div>
            """;

        Assert.That(WorkshopChangeHistoryVerifier.ParseLatest(html), Is.EqualTo("Newest player-facing note."));
    }

    [Test]
    public void UnknownProfileProperty_IsRejected()
    {
        var root = TestRepository.FindRoot();
        var source = Path.Combine(root, "mods", "GuestBedGizmo", "Release", "release.json");
        var temporary = Path.Combine(Path.GetDirectoryName(source)!, $"release-{Guid.NewGuid():N}.json");
        File.WriteAllText(temporary, File.ReadAllText(source).Replace("\"schema\":", "\"unknown\": true, \"schema\":"));
        try
        {
            var error = Assert.Throws<ReleaseProfileException>(() => ReleaseProfileCatalog.Load(root, temporary));
            Assert.That(error!.Message, Does.Contain("unknown"));
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    [Test]
    public void ProfilePathEscapeAndMissingIdentity_AreRejectedBeforeSideEffects()
    {
        var root = TestRepository.FindRoot();
        var source = Path.Combine(root, "mods", "GuestBedGizmo", "Release", "release.json");
        var json = File.ReadAllText(source);
        var escaped = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"release-{Guid.NewGuid():N}.json");
        File.WriteAllText(escaped, json.Replace("mods/GuestBedGizmo/GuestBedGizmo.csproj", "../outside.csproj"));
        try
        {
            Assert.That(
                Assert.Throws<ReleaseProfileException>(() => ReleaseProfileCatalog.Load(root, escaped))!.Message,
                Does.Contain("escapes"));
        }
        finally
        {
            File.Delete(escaped);
        }

        var noIdentity = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"release-{Guid.NewGuid():N}.json");
        var missingIdentityJson = System.Text.RegularExpressions.Regex.Replace(
            json,
            "\"publishedFileId\"\\s*:\\s*(null|\"[^\"]+\")",
            "\"publishedFileId\": null");
        missingIdentityJson = System.Text.RegularExpressions.Regex.Replace(
            missingIdentityJson,
            "\"allowFirstPublication\"\\s*:\\s*(true|false)",
            "\"allowFirstPublication\": false");
        File.WriteAllText(noIdentity, missingIdentityJson);
        try
        {
            Assert.That(
                Assert.Throws<ReleaseProfileException>(() => ReleaseProfileCatalog.Load(root, noIdentity))!.Message,
                Does.Contain("publishedFileId"));
        }
        finally
        {
            File.Delete(noIdentity);
        }
    }

    [Test]
    public void PackageIdTraversal_IsRejectedBeforeItCanSelectArtifactOrInstallPaths()
    {
        var root = TestRepository.FindRoot();
        var source = Path.Combine(root, "mods", "GuestBedGizmo", "Release", "release.json");
        var temporary = Path.Combine(Path.GetDirectoryName(source)!, $"release-{Guid.NewGuid():N}.json");
        File.WriteAllText(temporary, File.ReadAllText(source)
            .Replace("\"packageId\": \"fumblesneeze.guestbedgizmo\"", "\"packageId\": \"../escaped\""));
        try
        {
            Assert.That(
                Assert.Throws<ReleaseProfileException>(() => ReleaseProfileCatalog.Load(root, temporary))!.Message,
                Does.Contain("canonical"));
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    [Test]
    public void EmptySteamRequiredItemGraph_IsValidForAUniversalProfile()
    {
        var root = TestRepository.FindRoot();
        var source = Path.Combine(root, "mods", "GuestBedGizmo", "Release", "release.json");
        var temporary = Path.Combine(Path.GetDirectoryName(source)!, $"release-{Guid.NewGuid():N}.json");
        var json = System.Text.RegularExpressions.Regex.Replace(
            File.ReadAllText(source),
            "\\\"requiredWorkshopItems\\\"\\s*:\\s*\\[[^\\]]*\\]",
            "\"requiredWorkshopItems\": []",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        File.WriteAllText(temporary, json);
        try
        {
            var profile = ReleaseProfileCatalog.Load(root, temporary);
            Assert.That(profile.RequiredWorkshopItems, Is.Empty);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
