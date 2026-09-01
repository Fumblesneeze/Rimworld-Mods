using NUnit.Framework;
using System.Net;
using System.Text;
using System.Text.Json;

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
    public void Remote_baseline_binds_links_and_plan_matching_preserves_unrelated_keys()
    {
        WorkshopRemoteBaseline Create(WorkshopLink[] links) => WorkshopRemoteBaseline.Create(
            "7", "title", "description", 11, ["1.6"], "metadata", "Public",
            "https://images.steamusercontent.com/ugc/1/PRIMARY/", "PRIMARY", "9", 294100,
            100, 200, [], [], [], [], links);
        var expected = new[] { new WorkshopLink("github", WorkshopLinkPolicy.RepositoryUrl) };
        var baseline = Create(expected);
        var changed = Create([new WorkshopLink("github", WorkshopLinkPolicy.RepositoryUrl + "/issues")]);
        using var matching = JsonDocument.Parse($$"""
            { "RemoteLinks": [
                { "Key": "twitter", "Url": "https://example.invalid/profile" },
                { "Key": "github", "Url": "{{WorkshopLinkPolicy.RepositoryUrl}}" }
            ] }
            """);
        using var duplicate = JsonDocument.Parse($$"""
            { "RemoteLinks": [
                { "Key": "github", "Url": "{{WorkshopLinkPolicy.RepositoryUrl}}" },
                { "Key": "GitHub", "Url": "{{WorkshopLinkPolicy.RepositoryUrl}}" }
            ] }
            """);
        using var conflicting = JsonDocument.Parse($$"""
            { "RemoteLinks": [
                { "Key": "github", "Url": "{{WorkshopLinkPolicy.RepositoryUrl}}" },
                { "Key": "GitHub", "Url": "https://example.invalid/other" }
            ] }
            """);

        Assert.Multiple(() =>
        {
            Assert.That(baseline.Links, Is.EqualTo(expected));
            Assert.That(changed.StateDigest, Is.Not.EqualTo(baseline.StateDigest));
            Assert.That(ReleasePublisher.RemoteLinksMatchPlan(matching.RootElement, expected), Is.True);
            Assert.That(ReleasePublisher.RemoteLinksMatchPlan(duplicate.RootElement, expected), Is.False);
            Assert.That(ReleasePublisher.RemoteLinksMatchPlan(conflicting.RootElement, expected), Is.False);
        });
    }

    [Test]
    public void Already_submitted_plan_can_recover_only_when_unsupported_links_are_absent()
    {
        var github = new[] { new WorkshopLink("github", WorkshopLinkPolicy.RepositoryUrl) };
        using var absent = JsonDocument.Parse("""{ "RemoteLinks": [] }""");
        using var misleadingHiddenTag = JsonDocument.Parse($$"""
            { "RemoteLinks": [{ "Key": "github", "Url": "{{WorkshopLinkPolicy.RepositoryUrl}}" }] }
            """);
        using var missingSupported = JsonDocument.Parse("""{ "RemoteLinks": [] }""");

        Assert.Multiple(() =>
        {
            Assert.That(ReleasePublisher.RemoteLinksMatchPlan(absent.RootElement, github), Is.False);
            Assert.That(ReleasePublisher.RemoteLinksMatchRecovery(absent.RootElement, github), Is.True);
            Assert.That(ReleasePublisher.RemoteLinksMatchRecovery(misleadingHiddenTag.RootElement, github), Is.False);
            Assert.That(ReleasePublisher.RemoteLinksMatchRecovery(
                missingSupported.RootElement,
                [new WorkshopLink("reddit", "https://reddit.com/r/RimWorld")]), Is.False);
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
    public void Community_fallback_preserves_one_API_declared_hidden_slot_only_when_visible_URL_anchors_align()
    {
        var first = "https://images.steamusercontent.com/ugc/201/FIRST/";
        var last = "https://images.steamusercontent.com/ugc/203/LAST/";
        var twoVisible = $$"""
            <script>var rgFullScreenshotURLs = [
                { 'url': '{{first}}' },
                { 'url': '{{last}}' }
            ];</script>
            """;

        Assert.Multiple(() =>
        {
            Assert.That(
                WorkshopRemoteBaseline.ParseCommunityImagePreviewUrls(twoVisible, 2, 3),
                Is.EqualTo(new[] { first, last }));
            Assert.That(
                WorkshopRemoteBaseline.SelectAdditionalImageUrls(
                    [first, "", last],
                    [first + "?imw=5000", last + "?imw=5000"]),
                Is.EqualTo(new[] { first + "?imw=5000", "", last + "?imw=5000" }));
            Assert.That(
                () => WorkshopRemoteBaseline.SelectAdditionalImageUrls(
                    ["", ""],
                    [first]),
                Throws.InvalidOperationException.With.Message.Contains("unambiguously align"));
        });
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

    [Test]
    public async Task Community_evidence_uses_the_reviewed_browser_request_shape()
    {
        string? observedUserAgent = null;
        string? observedAcceptLanguage = null;
        string? observedAccept = null;
        using var client = new HttpClient(new ResponseHandler(request =>
        {
            observedUserAgent = request.Headers.UserAgent.ToString();
            observedAcceptLanguage = request.Headers.AcceptLanguage.ToString();
            observedAccept = request.Headers.Accept.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Encoding.ASCII.GetBytes("page"))
            };
        }));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("RimWorldModding.Mcp/0.1");

        _ = await WorkshopRemoteBaseline.DownloadBoundedAsync(
            client,
            "https://steamcommunity.com/sharedfiles/filedetails/?id=3782589902",
            4,
            "steamcommunity.com",
            "/sharedfiles/filedetails/",
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(observedUserAgent, Does.StartWith("Mozilla/5.0"));
            Assert.That(observedAcceptLanguage, Does.Contain("en-US"));
            Assert.That(observedAccept, Does.Contain("text/html"));
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
