using NUnit.Framework;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class WorkshopInlineImagesTests
{
    [TestCase(false)]
    [TestCase(true)]
    [TestCase(false, true)]
    public async Task Inline_images_require_exact_mirrored_committed_bytes(bool corruptRemote, bool editSourceAfterFreeze = false)
    {
        var root = Path.Combine(Path.GetTempPath(), "inline-images-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "mods"));
        Directory.CreateDirectory(Path.Combine(root, "openspec"));
        File.WriteAllText(Path.Combine(root, "AGENTS.md"), "test fixture");
        try
        {
            var bytes = new byte[] { 1, 2, 3, 4 };
            var path = Path.Combine(root, "card.png");
            await File.WriteAllBytesAsync(path, bytes);
            async Task<string> Git(params string[] args)
            {
                var result = await ProcessRunner.RunAsync("git", args, root, TimeSpan.FromSeconds(10), CancellationToken.None);
                Assert.That(result.ExitCode, Is.Zero, result.StandardError);
                return result.StandardOutput.Trim();
            }
            await Git("init");
            await Git("add", "card.png");
            await Git("-c", "user.name=Test", "-c", "user.email=test@example.invalid", "commit", "-m", "Image fixture");
            var revision = await Git("rev-parse", "HEAD");
            var url = $"https://raw.githubusercontent.com/Fumblesneeze/Rimworld-Mods/{revision}/card.png";
            var manifest = Path.Combine(root, "description-images.json");
            await File.WriteAllTextAsync(manifest, JsonSerializer.Serialize(new
            {
                schema = "WorkshopDescriptionImages/v1",
                images = new[] { new { path = "card.png", url, sha256 = Convert.ToHexString(SHA256.HashData(bytes)) } }
            }));
            var description = Path.Combine(root, "description.bbcode");
            await File.WriteAllTextAsync(description, $"[img]{url}[/img]");
            string? frozenRoot = null;
            if (editSourceAfterFreeze)
            {
                frozenRoot = Path.Combine(root, "frozen-inline");
                _ = WorkshopInlineImages.FreezeInputs(root, manifest, frozenRoot);
                manifest = Path.Combine(frozenRoot, "description-images.json");
                await File.WriteAllBytesAsync(path, [8, 8]);
                await File.WriteAllTextAsync(Path.Combine(root, "description-images.json"), "edited source");
            }
            using var http = new HttpClient(new ImageHandler(corruptRemote ? [9, 9] : bytes));
            if (corruptRemote)
                Assert.That(async () => await WorkshopInlineImages.ValidateAsync(root, description, manifest, http, CancellationToken.None),
                    Throws.InvalidOperationException.With.Message.Contains("mirrored bytes"));
            else
                Assert.That(async () => await WorkshopInlineImages.ValidateAsync(root, description, manifest, http, CancellationToken.None, frozenRoot), Throws.Nothing);
        }
        finally
        {
            foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(root, true);
        }
    }

    private sealed class ImageHandler(byte[] bytes) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) });
    }
}
