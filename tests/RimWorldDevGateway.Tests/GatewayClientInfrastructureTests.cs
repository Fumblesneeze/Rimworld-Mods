using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.RegularExpressions;
using RimWorldDevGateway.Client;
using RimWorldDevGateway.Contracts;
using NUnit.Framework;
using Verse;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayClientInfrastructureTests
{
    [Test]
    public void Http_transport_sends_headers_and_body_then_returns_the_bounded_response()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var received = new TaskCompletionSource<string>();
        var server = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
            var requestLine = await reader.ReadLineAsync();
            var authorization = string.Empty;
            var contentLength = 0;
            string? line;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
            {
                if (line.StartsWith("Authorization:", StringComparison.OrdinalIgnoreCase))
                {
                    authorization = line.Substring(line.IndexOf(':') + 1).Trim();
                }

                if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                {
                    contentLength = int.Parse(line.Substring(line.IndexOf(':') + 1).Trim());
                }
            }

            var bodyBuffer = new char[contentLength];
            var charactersRead = 0;
            while (charactersRead < bodyBuffer.Length)
            {
                var count = await reader.ReadAsync(bodyBuffer, charactersRead, bodyBuffer.Length - charactersRead);
                if (count == 0)
                {
                    break;
                }

                charactersRead += count;
            }

            received.SetResult($"{requestLine}|{authorization}|{new string(bodyBuffer, 0, charactersRead)}");
            var responseBody = Encoding.UTF8.GetBytes("{\"ok\":true}");
            var responseHeaders = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {responseBody.Length}\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(responseHeaders, 0, responseHeaders.Length);
            await stream.WriteAsync(responseBody, 0, responseBody.Length);
        });

        var transport = new GatewayHttpTransport(timeoutMilliseconds: 5_000, maxResponseBytes: 1024);
        var response = transport.Send(new GatewayClientHttpRequest(
            "POST",
            new Uri($"http://127.0.0.1:{port}/api/v1/actions/test"),
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["Authorization"] = "Bearer exact-token"
            },
            "application/json",
            Encoding.UTF8.GetBytes("{\"value\":7}")));

        Assert.That(server.Wait(TimeSpan.FromSeconds(5)), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(received.Task.Result, Is.EqualTo(
                "POST /api/v1/actions/test HTTP/1.1|Bearer exact-token|{\"value\":7}"));
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(response.ContentType, Does.StartWith("application/json"));
            Assert.That(Encoding.UTF8.GetString(response.Body), Is.EqualTo("{\"ok\":true}"));
        });
        listener.Stop();
    }

    [Test]
    public void Source_compiler_builds_a_net48_PE_against_RimWorld_and_contracts_then_removes_its_workspace()
    {
        var fixtureRoot = Path.Combine(Path.GetTempPath(), "gateway-client-test-" + Guid.NewGuid().ToString("N"));
        var compilerRoot = Path.Combine(fixtureRoot, "compiler");
        var sourcePath = Path.Combine(fixtureRoot, "Snippet.cs");
        Directory.CreateDirectory(fixtureRoot);
        File.WriteAllText(sourcePath, @"
using RimWorldDevGateway.Contracts;
using Steamworks;
using Verse;

public static class TestSnippet
{
    public static string Execute(string requestJson)
    {
        return typeof(Pawn).Name + typeof(SteamUGC).Name + new GatewaySessionManifest().ApiVersion + requestJson;
    }
}");

        try
        {
            var compiler = new DotNetGatewaySourceCompiler(temporaryRoot: compilerRoot);
            var assembly = compiler.Compile(new GatewaySourceCompilationRequest(
                sourcePath,
                Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!,
                typeof(GatewaySessionManifest).Assembly.Location));

            Assert.Multiple(() =>
            {
                Assert.That(assembly.Length, Is.GreaterThan(128));
                Assert.That(assembly[0], Is.EqualTo((byte)'M'));
                Assert.That(assembly[1], Is.EqualTo((byte)'Z'));
                Assert.That(Directory.Exists(compilerRoot), Is.True);
                Assert.That(Directory.GetFileSystemEntries(compilerRoot), Is.Empty);
            });
        }
        finally
        {
            if (Directory.Exists(fixtureRoot))
            {
                Directory.Delete(fixtureRoot, true);
            }
        }
    }

    [Test]
    public void Source_compiler_builds_the_checked_in_Steam_Workshop_publisher()
    {
        var root = FindRepositoryRoot();
        var compilerRoot = Path.Combine(Path.GetTempPath(), "gateway-release-compiler-" + Guid.NewGuid().ToString("N"));
        try
        {
            var compiler = new DotNetGatewaySourceCompiler(temporaryRoot: compilerRoot);
            var assembly = compiler.Compile(new GatewaySourceCompilationRequest(
                Path.Combine(root, "scripts", "Fixtures", "GatewaySteamWorkshopPublisher.cs"),
                Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!,
                typeof(GatewaySessionManifest).Assembly.Location));

            Assert.Multiple(() =>
            {
                Assert.That(assembly.Length, Is.GreaterThan(128));
                Assert.That(assembly[0], Is.EqualTo((byte)'M'));
                Assert.That(assembly[1], Is.EqualTo((byte)'Z'));
            });
        }
        finally
        {
            if (Directory.Exists(compilerRoot))
            {
                Directory.Delete(compilerRoot, true);
            }
        }
    }

    [Test]
    public void Checked_in_Steam_publisher_parses_the_bounded_primitive_Workshop_link_wire()
    {
        var root = FindRepositoryRoot();
        var compilerRoot = Path.Combine(Path.GetTempPath(), "gateway-release-link-wire-" + Guid.NewGuid().ToString("N"));
        const string requestJson =
            "{\"operation\":\"status\",\"workshopLinkJson\":[\"{\\\"key\\\":\\\"reddit\\\",\\\"url\\\":\\\"https://reddit.com/r/RimWorld\\\"}\"]}";
        try
        {
            var parsedGatewayArguments = new Dictionary<string, object?>
            {
                ["operation"] = "status",
                ["workshopLinkJson"] = new object[]
                {
                    "{\"key\":\"reddit\",\"url\":\"https://reddit.com/r/RimWorld\"}"
                }
            };
            Assert.DoesNotThrow(() => GatewayContractJson.Write(new GatewayAutomationRunRequest
            {
                Arguments = parsedGatewayArguments
            }), "The exact CLI request must remain serializable through the legacy net48 Gateway contract.");

            var compiler = new DotNetGatewaySourceCompiler(temporaryRoot: compilerRoot);
            var bytes = compiler.Compile(new GatewaySourceCompilationRequest(
                Path.Combine(root, "scripts", "Fixtures", "GatewaySteamWorkshopPublisher.cs"),
                Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!,
                typeof(GatewaySessionManifest).Assembly.Location));
            var assembly = Assembly.Load(bytes);
            var publisher = assembly.GetType("GatewaySteamWorkshopPublisher.Publisher", throwOnError: true)!;
            var requestType = publisher.GetNestedType("Request", BindingFlags.NonPublic)!;
            var parse = requestType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static)!;
            var request = parse.Invoke(null, new object[] { requestJson })!;
            var links = ((System.Collections.IEnumerable)requestType.GetField("WorkshopLinks")!.GetValue(request)!)
                .Cast<object>()
                .ToArray();

            Assert.That(links, Has.Length.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(links[0].GetType().GetField("Key")!.GetValue(links[0]), Is.EqualTo("reddit"));
                Assert.That(
                    links[0].GetType().GetField("Url")!.GetValue(links[0]),
                    Is.EqualTo("https://reddit.com/r/RimWorld"));
            });
        }
        finally
        {
            if (Directory.Exists(compilerRoot)) Directory.Delete(compilerRoot, true);
        }
    }

    [Test]
    public void Checked_in_Steam_publisher_safety_classifies_failures_and_correlates_callback_IDs()
    {
        var root = FindRepositoryRoot();
        var compilerRoot = Path.Combine(Path.GetTempPath(), "gateway-release-safety-" + Guid.NewGuid().ToString("N"));
        try
        {
            var compiler = new DotNetGatewaySourceCompiler(temporaryRoot: compilerRoot);
            var bytes = compiler.Compile(new GatewaySourceCompilationRequest(
                Path.Combine(root, "scripts", "Fixtures", "GatewaySteamWorkshopPublisher.cs"),
                Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!,
                typeof(GatewaySessionManifest).Assembly.Location));
            var assembly = Assembly.Load(bytes);
            var safety = assembly.GetType("GatewaySteamWorkshopPublisher.PublisherSafety", throwOnError: true)!;
            var visibility = safety.GetMethod("EffectiveVisibility", BindingFlags.Public | BindingFlags.Static)!;
            var previewOperations = safety.GetMethod("PreviewOperations", BindingFlags.Public | BindingFlags.Static)!;
            var mutationStage = safety.GetMethod("MutationStageForOperation", BindingFlags.Public | BindingFlags.Static)!;
            var publisherSource = File.ReadAllText(Path.Combine(root, "scripts", "Fixtures", "GatewaySteamWorkshopPublisher.cs"));
            Assert.Multiple(() =>
            {
                Assert.That(
                    visibility.Invoke(null, new object[] { true, "Public" }),
                    Is.EqualTo("Private"),
                    "A newly created item must stay private for its first human review.");
                Assert.That(
                    visibility.Invoke(null, new object[] { false, "Public" }),
                    Is.EqualTo("Public"),
                    "An existing item update must retain the reviewed release visibility.");
                Assert.That(
                    (string[])previewOperations.Invoke(null, new object[] { 2u, 3 })!,
                    Is.EqualTo(new[] { "remove:1", "remove:0", "add:0", "add:1", "add:2" }),
                    "Preview reconciliation must discard every stale Steam slot before rebuilding the exact ordered gallery.");
                Assert.That(
                    (string[])previewOperations.Invoke(null, new object[] { 4u, 2 })!,
                    Is.EqualTo(new[] { "remove:3", "remove:2", "remove:1", "remove:0", "add:0", "add:1" }),
                    "Every old preview must be removed from the end before the reviewed gallery is recreated.");
                Assert.That(
                    mutationStage.Invoke(null, new object[] { "preview-sync", "submit" }),
                    Is.EqualTo("preview-submit"),
                    "A preview callback failure must persist in the preview retry-state family.");
                Assert.That(
                    mutationStage.Invoke(null, new object[] { "preview-sync", "submit-setter-Preview-add:0" }),
                    Is.EqualTo("preview-submit-setter-Preview-add:0"),
                    "A preview setter failure must remain a definite preview failure.");
                Assert.That(
                    mutationStage.Invoke(null, new object[] { "publish", "submit" }),
                    Is.EqualTo("submit"),
                    "Ordinary content publication must retain its existing state family.");
                Assert.That(
                    Regex.Matches(publisherSource, @"SteamUGC\.SubmitItemUpdate\(handle, request\.ChangeNote\)").Count,
                    Is.EqualTo(1),
                    "Only the final content publication may consume the authored player-facing change note.");
                Assert.That(
                    publisherSource,
                    Does.Contain("SteamUGC.SubmitItemUpdate(handle, null)"),
                    "Preview-only synchronization must disable Steam change-note creation.");
                Assert.That(publisherSource, Does.Contain("SteamUGC.RemoveItemKeyValueTags"));
                Assert.That(publisherSource, Does.Contain("SteamUGC.AddItemKeyValueTag"));
                Assert.That(publisherSource, Does.Contain("SteamUGC.SetReturnKeyValueTags"));
                Assert.That(publisherSource, Does.Contain("SteamUGC.GetQueryUGCKeyValueTag"));
                Assert.That(publisherSource, Does.Contain("RemoteLinks"));
                Assert.That(publisherSource, Does.Not.Match(@"SteamUGC\.SubscribeItem\("),
                    "The generic release surface must not subscribe to a product item.");
            });
            var failure = safety.GetMethod("DurableFailureStage", BindingFlags.Public | BindingFlags.Static)!;
            var correlation = safety.GetMethod("CallbackIdsMatch", BindingFlags.Public | BindingFlags.Static)!;
            var invalid = safety.GetMethod("IsInvalidCallHandle", BindingFlags.Public | BindingFlags.Static)!;
            var ownerAbsence = safety.GetMethod("OwnerScanProvesAbsence", BindingFlags.Public | BindingFlags.Static)!;
            var subscribed = safety.GetMethod("IsSubscribedState", BindingFlags.Public | BindingFlags.Static)!;

            Assert.Multiple(() =>
            {
                Assert.That(failure.Invoke(null, new object[] { "create", true }), Is.EqualTo("create-indeterminate"));
                Assert.That(failure.Invoke(null, new object[] { "create", false }), Is.EqualTo("create-failed-definite"));
                Assert.That(failure.Invoke(null, new object[] { "submit", true }), Is.EqualTo("submit-indeterminate"));
                Assert.That(failure.Invoke(null, new object[] { "preview-submit", true }), Is.EqualTo("preview-submit-indeterminate"));
                Assert.That(failure.Invoke(null, new object[] { "dependency-remove", false }), Is.EqualTo("dependency-failed-definite"));
                Assert.That(failure.Invoke(null, new object[] { "app-dependency-add", true }), Is.EqualTo("dependency-indeterminate"));
                Assert.That(correlation.Invoke(null, new object[] { 17UL, 23UL, 17UL, 23UL }), Is.True);
                Assert.That(correlation.Invoke(null, new object[] { 17UL, 23UL, 17UL, 99UL }), Is.False);
                Assert.That(invalid.Invoke(null, new object[] { 0UL }), Is.True);
                Assert.That(invalid.Invoke(null, new object[] { 1UL }), Is.False);
                Assert.That(ownerAbsence.Invoke(null, new object[] { 0u, 0u, 0 }), Is.True);
                Assert.That(ownerAbsence.Invoke(null, new object[] { 2u, 1u, 0 }), Is.False);
                Assert.That(ownerAbsence.Invoke(null, new object[] { 1u, 1u, 1 }), Is.False);
                Assert.That(subscribed.Invoke(null, new object[] { 0u }), Is.False);
                Assert.That(subscribed.Invoke(null, new object[] { 1u }), Is.True);
                Assert.That(subscribed.Invoke(null, new object[] { 5u }), Is.True);
                Assert.That(subscribed.Invoke(null, new object[] { 4u }), Is.False,
                    "A cached installed copy is not itself an active Workshop subscription.");
                Assert.That(publisherSource, Does.Contain("SteamUGC.UnsubscribeItem"));
                Assert.That(publisherSource, Does.Contain("RemoteStorageUnsubscribePublishedFileResult_t"));
                Assert.That(publisherSource, Does.Contain("unsubscribe-callback-confirmed"));
            });
        }
        finally
        {
            if (Directory.Exists(compilerRoot)) Directory.Delete(compilerRoot, true);
        }
    }

    [Test]
    public void Checked_in_Steam_publisher_queries_and_mutates_exact_application_dependencies()
    {
        var root = FindRepositoryRoot();
        var compilerRoot = Path.Combine(Path.GetTempPath(), "gateway-release-app-dependencies-" + Guid.NewGuid().ToString("N"));
        try
        {
            var sourcePath = Path.Combine(root, "scripts", "Fixtures", "GatewaySteamWorkshopPublisher.cs");
            var compiler = new DotNetGatewaySourceCompiler(temporaryRoot: compilerRoot);
            var assembly = Assembly.Load(compiler.Compile(new GatewaySourceCompilationRequest(
                sourcePath,
                Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!,
                typeof(GatewaySessionManifest).Assembly.Location)));
            var source = File.ReadAllText(sourcePath);
            var safety = assembly.GetType("GatewaySteamWorkshopPublisher.PublisherSafety", throwOnError: true)!;
            var correlation = safety.GetMethod("CallbackAppIdsMatch", BindingFlags.Public | BindingFlags.Static);
            var existingIdentity = safety.GetMethod("ExistingItemIdentityMatches", BindingFlags.Public | BindingFlags.Static);
            var exactChildInventory = safety.GetMethod("ExactChildInventoryFits", BindingFlags.Public | BindingFlags.Static);

            Assert.Multiple(() =>
            {
                Assert.That(correlation, Is.Not.Null);
                Assert.That(existingIdentity, Is.Not.Null);
                Assert.That(exactChildInventory, Is.Not.Null);
                Assert.That(correlation!.Invoke(null, new object[] { 17UL, 1392840u, 17UL, 1392840u }), Is.True);
                Assert.That(correlation.Invoke(null, new object[] { 17UL, 1392840u, 17UL, 1149640u }), Is.False);
                Assert.That(existingIdentity!.Invoke(null, new object[] { 17UL, 23UL, 294100u, 17UL, 23UL, 294100u }), Is.True);
                Assert.That(existingIdentity.Invoke(null, new object[] { 17UL, 23UL, 294100u, 17UL, 99UL, 294100u }), Is.False);
                Assert.That(exactChildInventory!.Invoke(null, new object[] { 64u }), Is.True);
                Assert.That(exactChildInventory.Invoke(null, new object[] { 65u }), Is.False,
                    "An oversized Workshop dependency inventory must fail closed instead of truncating stale children.");
                Assert.That(source, Does.Contain("SteamUGC.GetAppDependencies"));
                Assert.That(source, Does.Contain("SteamUGC.AddAppDependency"));
                Assert.That(source, Does.Contain("SteamUGC.RemoveAppDependency"));
                Assert.That(source, Does.Contain("RemoteAppDependencies"));
                Assert.That(source, Does.Contain("requiredDlcAppId"));
            });
        }
        finally
        {
            if (Directory.Exists(compilerRoot)) Directory.Delete(compilerRoot, true);
        }
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "RimWorldMods.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
