using System.Text.Json;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class WorkshopPresentationSynchronizerTests
{
    [Test]
    public void Preview_plan_keeps_title_first_and_exact_declared_card_order()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "presentation-plan-" + Guid.NewGuid().ToString("N"));
        var assets = Path.Combine(root, "assets");
        Directory.CreateDirectory(assets);
        var title = Path.Combine(root, "preview-main.png");
        File.WriteAllBytes(title, [1, 2, 3]);
        var tokens = new[] { "kitchenware", "teamwork", "dishwashing", "meals", "colony", "compatibility" };
        foreach (var token in tokens) File.WriteAllBytes(Path.Combine(assets, $"feature-{token}.png"), [4, 5, 6]);
        File.WriteAllText(Path.Combine(root, "presentation.json"), JsonSerializer.Serialize(new
        {
            schema = "ImmersiveChefs/WorkshopPresentation/v1",
            carouselCards = tokens,
            cards = tokens.Select(token => new { token, path = $"assets/feature-{token}.png" })
        }));

        var plan = WorkshopPresentationSynchronizer.ReadPreviewInputs(root, title);

        Assert.That(plan.Select(item => item.Token), Is.EqualTo(new[] { "immersive-chefs" }.Concat(tokens)));
        Assert.That(plan.Select(item => item.Index), Is.EqualTo(Enumerable.Range(0, 7)));
        Assert.That(plan.Select(item => item.Path), Has.All.Matches<string>(Path.IsPathFullyQualified));
    }

    [Test]
    public void Resolved_presentation_rejects_an_unreachable_inline_card()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "presentation-links-" + Guid.NewGuid().ToString("N"));
        var assets = Path.Combine(root, "assets");
        Directory.CreateDirectory(assets);
        var cardTokens = Enumerable.Range(1, 6).Select(index => $"card-{index}").ToArray();
        var previewRecords = Enumerable.Range(0, 7).Select(index =>
        {
            var localName = index == 0 ? "preview-main.png" : $"feature-card-{index}.png";
            var path = index == 0 ? Path.Combine(root, localName) : Path.Combine(assets, localName);
            File.WriteAllBytes(path, [(byte)(index + 1), 2, 3]);
            var url = $"https://images.steamusercontent.com/ugc/{123 + index}/ABC{index}/";
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            return new
            {
                token = index == 0 ? "immersive-chefs" : $"card-{index}",
                remoteIndex = index,
                remoteUrl = url,
                localPath = localName,
                localSha256 = hash,
                remoteSha256 = hash,
                remoteType = "k_EItemPreviewType_Image"
            };
        }).ToArray();
        File.WriteAllText(Path.Combine(root, "presentation.json"), JsonSerializer.Serialize(new
        {
            schema = "ImmersiveChefs/WorkshopPresentation/v1",
            carouselCards = cardTokens,
            cards = cardTokens.Select((token, index) => new
            {
                token,
                path = $"assets/feature-card-{index + 1}.png"
            })
        }));
        var description = Path.Combine(root, "description.bbcode");
        File.WriteAllText(description, string.Join("\n", previewRecords.Skip(1).Select(item => $"[img]{item.remoteUrl}[/img]")));
        var descriptionHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(description)));
        var provenance = Path.Combine(root, "description.provenance.json");
        File.WriteAllText(provenance, JsonSerializer.Serialize(new
        {
            schema = "ImmersiveChefs/WorkshopDescriptionProvenance/v1",
            publishedFileId = "3782589902",
            descriptionSha256 = descriptionHash,
            previews = previewRecords
        }));
        using var http = new HttpClient(new StaticHandler(HttpStatusCode.NotFound, [9]));

        Assert.That(async () => await WorkshopResolvedPresentation.ValidateAsync(
                description, provenance, root, "3782589902", http, CancellationToken.None),
            Throws.InvalidOperationException.With.Message.Contains("HTTP 404"));
    }

    [Test]
    public void Durable_preview_state_never_resubmits_an_indeterminate_update()
    {
        var plan = new string('A', 64);
        Assert.Multiple(() =>
        {
            Assert.That(WorkshopPresentationSynchronizer.DecideMutation(
                new WorkshopPresentationState("preview-submit-indeterminate", plan), plan, false), Is.EqualTo("resume"));
            Assert.That(WorkshopPresentationSynchronizer.DecideMutation(
                new WorkshopPresentationState("preview-submit-admitted", plan), plan, false), Is.EqualTo("resume"));
            Assert.That(WorkshopPresentationSynchronizer.DecideMutation(
                new WorkshopPresentationState("succeeded", plan), plan, true), Is.EqualTo("skip"));
            Assert.That(WorkshopPresentationSynchronizer.DecideMutation(
                new WorkshopPresentationState("succeeded", plan), plan, false), Is.EqualTo("submit"));
            Assert.That(WorkshopPresentationSynchronizer.DecideMutation(
                new WorkshopPresentationState("preview-submitted", plan), plan, true), Is.EqualTo("skip"));
            Assert.That(WorkshopPresentationSynchronizer.DecideMutation(
                new WorkshopPresentationState("preview-submitted", plan), plan, false), Is.EqualTo("submit"));
            Assert.That(WorkshopPresentationSynchronizer.DecideMutation(
                new WorkshopPresentationState("preview-submitted", new string('B', 64)), plan, false), Is.EqualTo("submit"));
            Assert.That(() => WorkshopPresentationSynchronizer.DecideMutation(
                    new WorkshopPresentationState("preview-submit-indeterminate", new string('B', 64)), plan, false),
                Throws.InvalidOperationException);
            Assert.That(() => WorkshopPresentationSynchronizer.DecideMutation(
                    new WorkshopPresentationState("preview-submit-admitted", new string('B', 64)), plan, false),
                Throws.InvalidOperationException);
            Assert.That(() => WorkshopPresentationSynchronizer.DecideMutation(
                    new WorkshopPresentationState("legal-agreement-required", plan), plan, false),
                Throws.InvalidOperationException);
        });
    }

    [Test]
    public void Preview_sync_request_matches_the_gateway_admission_contract()
    {
        var plan = new string('A', 64);
        var nonce = new string('B', 64);
        var request = WorkshopPresentationSynchronizer.BuildPreviewSyncRequest(
            plan, nonce, 3782589902, "PublishedFileId.txt", "presentation-preview-state.txt",
            "Immersive Chefs", ["title.png", "card.png"]);

        Assert.Multiple(() =>
        {
            Assert.That(request["operation"], Is.EqualTo("preview-sync"));
            Assert.That(request["confirmationNonce"], Is.EqualTo(nonce));
            Assert.That(request["confirmation"], Is.EqualTo($"publish {plan} {nonce}"));
            Assert.That(request["statePath"], Is.EqualTo("presentation-preview-state.txt"));
            Assert.That(request["changeNote"], Is.EqualTo(""));
        });
    }

    [Test]
    public void Authenticated_gallery_must_match_every_resolved_url_and_hash()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "presentation-gallery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var urls = Enumerable.Range(0, 7)
            .Select(index => $"https://images.steamusercontent.com/ugc/{index + 1}/HASH{index}/")
            .ToArray();
        var hashes = Enumerable.Range(0, 7).Select(index => new string((char)('A' + index), 64)).ToArray();
        var provenance = Path.Combine(root, "description.provenance.json");
        File.WriteAllText(provenance, JsonSerializer.Serialize(new
        {
            previews = Enumerable.Range(0, 7).Select(index => new
            {
                remoteUrl = urls[index],
                localSha256 = hashes[index]
            })
        }));
        var identities = Enumerable.Range(0, 7)
            .Select(index => $"{index}|card-{index}.png|k_EItemPreviewType_Image|{hashes[index]}")
            .ToArray();
        var baseline = WorkshopRemoteBaseline.Create(
            "3782589902", "Immersive Chefs", "description", 100, ["1.6"], "metadata", "Public",
            urls[0], hashes[0], "76561198077136238", 294100, 10, 20, [], [], identities, urls);

        WorkshopResolvedPresentation.AssertRemoteGallery(provenance, baseline);
        var stale = baseline with { AdditionalPreviewUrls = urls.Select((url, index) => index == 3 ? url + "stale/" : url).ToArray() };
        Assert.That(() => WorkshopResolvedPresentation.AssertRemoteGallery(provenance, stale),
            Throws.InvalidOperationException.With.Message.Contains("slot 3"));
    }

    [Test]
    public void Default_cli_projection_can_report_a_successful_external_sync()
    {
        var result = new WorkshopPresentationSyncResult(
            "presentation-synchronized", "fumblesneeze.immersivechefs", "3782589902",
            "https://steamcommunity.com/sharedfiles/filedetails/?id=3782589902", new string('A', 64),
            "inventory.json", 7, "description.bbcode", new string('B', 64), "description.provenance.json",
            false, "run", true);

        Assert.That(McpCli.DescribePresentationSync(result), Is.EqualTo(new[]
        {
            "status: presentation-synchronized",
            "item: 3782589902",
            "url: https://steamcommunity.com/sharedfiles/filedetails/?id=3782589902",
            "previews: 7",
            "description: description.bbcode",
            "inventory: inventory.json"
        }));
    }

    private sealed class StaticHandler(HttpStatusCode status, byte[] bytes) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                RequestMessage = request,
                Content = new ByteArrayContent(bytes)
            });
    }
}
