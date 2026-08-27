using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class SubscriberVerificationProfileTests
{
    [Test]
    public void RepositoryProfiles_ProduceDataDrivenSubscriberPlans()
    {
        var root = TestRepository.FindRoot();
        var profiles = ReleaseProfileCatalog.Discover(root);
        var guest = profiles.Single(profile => profile.PackageId == "fumblesneeze.guestbedgizmo");
        var immersive = profiles.Single(profile => profile.PackageId == "fumblesneeze.immersivechefs");

        var guestManifest = SubscriberVerificationProfiles.Load(root, guest.VerificationProfile);
        var guestPlan = SubscriberVerificationPlan.Create(root, guest, guestManifest, "1234567890");
        Assert.That(guestPlan.Engine, Is.EqualTo("gateway-automation"));
        Assert.That(guestPlan.PackageIds, Is.EqualTo(new[]
        {
            "brrainz.harmony",
            "ludeon.rimworld.ideology",
            "orion.hospitality",
            "fumblesneeze.guestbedgizmo"
        }));
        Assert.That(guestPlan.Steps.Select(step => step.Operation),
            Is.EqualTo(new[] { "arrange", "open", "choose", "observe", "cleanup" }));
        Assert.That(guestPlan.WorkshopPackagePath, Does.EndWith(Path.Combine("294100", "1234567890")));

        var immersiveManifest = SubscriberVerificationProfiles.Load(root, immersive.VerificationProfile);
        var immersivePlan = SubscriberVerificationPlan.Create(root, immersive, immersiveManifest, "3782589902");
        Assert.That(immersivePlan.Engine, Is.EqualTo("powershell-subscriber"));
        Assert.That(immersivePlan.Script, Does.EndWith("Invoke-ImmersiveChefsSubscribedSmoke.ps1"));
    }

    [Test]
    public void UnknownSubscriberProfileField_IsRejected()
    {
        var root = TestRepository.FindRoot();
        var source = Path.Combine(root, "mods", "GuestBedGizmo", "Release", "subscriber-verification.json");
        var temporary = Path.Combine(root, "artifacts", $"subscriber-{Guid.NewGuid():N}.json");
        File.WriteAllText(temporary, File.ReadAllText(source).Replace("\"schema\":", "\"unknown\": true, \"schema\":"));
        try
        {
            Assert.That(
                Assert.Throws<InvalidOperationException>(() => SubscriberVerificationProfiles.Load(root, temporary))!.Message,
                Does.Contain("unknown"));
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    [Test]
    public void MissingReferencedAutomationSource_IsRejectedDuringProfileLoading()
    {
        var root = TestRepository.FindRoot();
        var source = Path.Combine(root, "mods", "GuestBedGizmo", "Release", "subscriber-verification.json");
        var temporary = Path.Combine(root, "artifacts", $"subscriber-{Guid.NewGuid():N}.json");
        File.WriteAllText(temporary, File.ReadAllText(source)
            .Replace("tools/RimWorldModding.Mcp/Fixtures/GuestBedSubscriberVerification.cs.source", "artifacts/missing-subscriber.source"));
        try
        {
            Assert.That(
                Assert.Throws<InvalidOperationException>(() => SubscriberVerificationProfiles.Load(root, temporary))!.Message,
                Does.Contain("source"));
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    [Test]
    public void PowerShellSubscriberEvidencePath_IsReservedWithoutPrecreatingAdapterOwnedDirectory()
    {
        var root = TestRepository.FindRoot();
        var packageId = "test.subscriber." + Guid.NewGuid().ToString("N");
        var runId = "subscriber-path-" + Guid.NewGuid().ToString("N");
        var verifier = new SubscriberVerifier(root);
        var packageRoot = Path.Combine(root, "artifacts", "Releases", packageId);
        try
        {
            var path = verifier.PrepareEvidenceRoot(packageId, runId, createDirectory: false);

            Assert.Multiple(() =>
            {
                Assert.That(path, Does.StartWith(Path.Combine(root, "artifacts", "Releases") + Path.DirectorySeparatorChar));
                Assert.That(Directory.Exists(Path.GetDirectoryName(path)), Is.True,
                    "the adapter's parent directory must exist before process launch");
                Assert.That(Directory.Exists(path), Is.False,
                    "the frozen PowerShell adapter must acquire its own new output directory");
            });
        }
        finally
        {
            if (Directory.Exists(packageRoot)) Directory.Delete(packageRoot, recursive: true);
        }
    }

    [Test]
    public void FrozenPowerShellSubscriberAdapter_IsStagedWithRepositoryRootAsItsParent()
    {
        var root = TestRepository.FindRoot();
        var source = Path.Combine(root, "artifacts", "subscriber-source-" + Guid.NewGuid().ToString("N") + ".ps1");
        var runId = "adapter-" + Guid.NewGuid().ToString("N");
        File.WriteAllText(source, "$repositoryRoot = Join-Path $PSScriptRoot '..'\n");
        string? staged = null;
        try
        {
            staged = new SubscriberVerifier(root).StagePowerShellAdapter(source, runId);

            Assert.Multiple(() =>
            {
                Assert.That(File.ReadAllBytes(staged), Is.EqualTo(File.ReadAllBytes(source)));
                Assert.That(
                    Path.GetFullPath(Path.Combine(Path.GetDirectoryName(staged)!, "..")),
                    Is.EqualTo(Path.GetFullPath(root)).IgnoreCase,
                    "the frozen adapter's existing PSScriptRoot contract must still resolve the repository root");
                Assert.That(staged, Is.Not.EqualTo(source).IgnoreCase);
                Assert.That(
                    () => new SubscriberVerifier(root).StagePowerShellAdapter(source, runId),
                    Throws.TypeOf<IOException>(),
                    "a retained run-owned transient adapter must never be overwritten");
            });
        }
        finally
        {
            if (staged is not null && File.Exists(staged)) File.Delete(staged);
            if (File.Exists(source)) File.Delete(source);
        }
    }
}
