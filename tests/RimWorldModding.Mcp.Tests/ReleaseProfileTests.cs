using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class ReleaseProfileTests
{
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
        Assert.That(guest.Title, Is.EqualTo("Hospitality + Ideoligy Patch"));
        Assert.That(guest.PublishedFileId, Is.Null);
        Assert.That(guest.AllowFirstPublication, Is.True);
        Assert.That(guest.Visibility, Is.EqualTo("Private"));
        Assert.That(guest.RequiredWorkshopItems, Is.EquivalentTo(new[] { "2009463077", "3509486825" }));
        Assert.DoesNotThrow(() => ReleaseEnvironmentValidator.Validate(guest));
        Assert.DoesNotThrow(() => ReleaseChangeNotePolicy.Validate(guest));
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
    public void ExistingPrivateItem_IsNotUpdatedWithoutVerifiableChangeHistory()
    {
        var root = TestRepository.FindRoot();
        var guest = ReleaseProfileCatalog.Discover(root)
            .Single(profile => profile.PackageId == "fumblesneeze.guestbedgizmo") with
        {
            PublishedFileId = "1234567890",
            AllowFirstPublication = false,
            PreviousChangeNote = "Initial specific release note."
        };

        var error = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await WorkshopChangeHistoryVerifier.ValidatePreviousAsync(guest, CancellationToken.None));
        Assert.That(error!.Message, Does.Contain("private Workshop item"));
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
        File.WriteAllText(noIdentity, json.Replace("\"allowFirstPublication\": true", "\"allowFirstPublication\": false"));
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
