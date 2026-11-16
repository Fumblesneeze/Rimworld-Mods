using NUnit.Framework;
using System.Text.Json;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class SteamDepotAcquisitionTests
{
    [Test]
    public void Branch_download_rejects_an_unowned_existing_directory()
    {
        var target = Path.Combine(Path.GetTempPath(), "unowned-steam-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "keep.txt"), "user content");
        try
        {
            var registry = OperationRegistry.CreateDefault(TestRepository.FindRoot());
            Assert.That(async () => await registry.InvokeAsync("game_depot_download", JsonSerializer.SerializeToElement(new
            {
                targetId = "1.6", targetDirectory = target,
                downloaderPath = Path.Combine(target, "DepotDownloader.exe"), interactiveLogin = true
            }), CancellationToken.None), Throws.ArgumentException.With.Message.Contains("owned"));
            Assert.That(File.ReadAllText(Path.Combine(target, "keep.txt")), Is.EqualTo("user content"));
        }
        finally { Directory.Delete(target, true); }
    }

    [Test]
    public void Downloader_refuses_repository_content_before_starting_a_process()
    {
        var root = TestRepository.FindRoot();
        var registry = OperationRegistry.CreateDefault(root);
        Assert.That(async () => await registry.InvokeAsync("game_depot_download", JsonSerializer.SerializeToElement(new
        {
            targetId = "1.6", targetDirectory = Path.Combine(root, "artifacts", "game"),
            downloaderPath = Path.Combine(root, "DepotDownloader.exe"), interactiveLogin = true
        }), CancellationToken.None), Throws.ArgumentException.With.Message.Contains("external"));
    }
}
