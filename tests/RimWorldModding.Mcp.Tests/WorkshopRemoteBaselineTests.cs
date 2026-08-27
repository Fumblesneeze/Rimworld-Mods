using NUnit.Framework;
using System.Net;
using System.Text;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class WorkshopRemoteBaselineTests
{
    [Test]
    public void Community_fallback_reads_the_exact_ordered_Steam_image_inventory()
    {
        const string html = """
            <script>
            var unrelated = 'https://images.steamusercontent.com/ugc/1/IGNORE/';
            var rgFullScreenshotURLs = [
                { 'previewid' : '11', 'url': 'https://images.steamusercontent.com/ugc/101/AAA/?imw=5000&amp;letterbox=false' },
                { 'previewid' : '12', 'url': 'https://images.steamusercontent.com/ugc/102/BBB/?imw=5000&amp;letterbox=false' }
            ];
            </script>
            """;

        Assert.That(
            WorkshopRemoteBaseline.ParseCommunityImagePreviewUrls(html, 2),
            Is.EqualTo(new[]
            {
                "https://images.steamusercontent.com/ugc/101/AAA/?imw=5000&letterbox=false",
                "https://images.steamusercontent.com/ugc/102/BBB/?imw=5000&letterbox=false"
            }));
    }

    [Test]
    public void Community_fallback_fails_closed_on_count_or_host_drift()
    {
        const string oneImage = """
            <script>var rgFullScreenshotURLs = [
                { 'previewid' : '11', 'url': 'https://images.steamusercontent.com/ugc/101/AAA/' }
            ];</script>
            """;
        const string wrongHost = """
            <script>var rgFullScreenshotURLs = [
                { 'previewid' : '11', 'url': 'https://example.invalid/ugc/101/AAA/' }
            ];</script>
            """;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => WorkshopRemoteBaseline.ParseCommunityImagePreviewUrls(oneImage, 2),
                Throws.InvalidOperationException.With.Message.Contains("exact image preview inventory"));
            Assert.That(
                () => WorkshopRemoteBaseline.ParseCommunityImagePreviewUrls(wrongHost, 1),
                Throws.InvalidOperationException.With.Message.Contains("Steam CDN"));
        });
    }

    [Test]
    public void Remote_baseline_preserves_additional_preview_order()
    {
        WorkshopRemoteBaseline Create(string[] additional) => WorkshopRemoteBaseline.Create(
            "7", "title", "description", 11, ["1.6"], "metadata", "Public",
            "https://images.steamusercontent.com/ugc/1/PRIMARY/", "PRIMARY", "9", 294100,
            100, 200, [], [], additional);

        var first = Create(["0|first.png|Image|AAA", "1|second.png|Image|BBB"]);
        var reversed = Create(["1|second.png|Image|BBB", "0|first.png|Image|AAA"]);

        Assert.Multiple(() =>
        {
            Assert.That(first.AdditionalPreviews,
                Is.EqualTo(new[] { "0|first.png|Image|AAA", "1|second.png|Image|BBB" }));
            Assert.That(reversed.StateDigest, Is.Not.EqualTo(first.StateDigest));
        });
    }

    [Test]
    public void Community_fallback_replaces_the_complete_authenticated_inventory()
    {
        var authenticated = new[]
        {
            "https://images.steamusercontent.com/ugc/101/STALE/",
            ""
        };
        var community = new[]
        {
            "https://images.steamusercontent.com/ugc/201/FIRST/",
            "https://images.steamusercontent.com/ugc/202/SECOND/"
        };

        Assert.That(
            WorkshopRemoteBaseline.SelectAdditionalImageUrls(authenticated, community),
            Is.EqualTo(community));
    }

    [Test]
    public void Empty_or_non_image_preview_inventories_need_no_image_fallback()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                WorkshopRemoteBaseline.SelectAdditionalImageUrls([], []),
                Is.Empty);
            Assert.That(
                () => WorkshopRemoteBaseline.SelectAdditionalImageUrls(
                    [], ["https://images.steamusercontent.com/ugc/201/UNEXPECTED/"]),
                Throws.InvalidOperationException.With.Message.Contains("empty authenticated preview inventory"));
        });
    }

    [Test]
    public async Task Remote_evidence_download_is_origin_locked_redirect_safe_and_bounded()
    {
        const string admitted = "https://images.steamusercontent.com/ugc/101/AAA/";
        using var exactClient = new HttpClient(new ResponseHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Encoding.ASCII.GetBytes("1234"))
            }));
        using var redirectClient = new HttpClient(new ResponseHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Redirect)
            {
                Headers = { Location = new Uri("https://example.invalid/stolen") }
            }));
        using var declaredOversizeClient = new HttpClient(new ResponseHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Encoding.ASCII.GetBytes("12345"))
            };
            response.Content.Headers.ContentLength = 5;
            return response;
        }));

        var exact = await WorkshopRemoteBaseline.DownloadBoundedAsync(
            exactClient, admitted, 4, "images.steamusercontent.com", "/ugc/", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(exact), Is.EqualTo("1234"));
            Assert.That(async () => await WorkshopRemoteBaseline.DownloadBoundedAsync(
                    exactClient, "https://example.invalid/ugc/101/AAA/", 4,
                    "images.steamusercontent.com", "/ugc/", CancellationToken.None),
                Throws.InvalidOperationException.With.Message.Contains("outside the admitted HTTPS origin"));
            Assert.That(async () => await WorkshopRemoteBaseline.DownloadBoundedAsync(
                    redirectClient, admitted, 4,
                    "images.steamusercontent.com", "/ugc/", CancellationToken.None),
                Throws.InvalidOperationException.With.Message.Contains("redirect"));
            Assert.That(async () => await WorkshopRemoteBaseline.DownloadBoundedAsync(
                    declaredOversizeClient, admitted, 4,
                    "images.steamusercontent.com", "/ugc/", CancellationToken.None),
                Throws.InvalidOperationException.With.Message.Contains("reviewed byte bound"));
        });
    }

    private sealed class ResponseHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(responder(request));
    }
}
