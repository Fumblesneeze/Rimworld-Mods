using System.Collections.Generic;
using System.IO;
using System.Text;
using RimWorldDevGateway.Client;
using RimWorldDevGateway.Contracts;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayClientCliTests
{
    [Test]
    public void Help_is_successful_and_documents_commands_output_modes_and_exit_codes()
    {
        var fixture = new CliFixture();

        var exitCode = fixture.App.Run(new[] { "--help" }, fixture.Output, fixture.Error);

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.Zero);
            Assert.That(fixture.Output.ToString(), Does.Contain("execute-source"));
            Assert.That(fixture.Output.ToString(), Does.Contain("quickstart"));
            Assert.That(fixture.Output.ToString(), Does.Contain("-o|--output json|table"));
            Assert.That(fixture.Output.ToString(), Does.Contain("Exit codes: 0 success, 1 runtime failure, 2 invalid usage."));
            Assert.That(fixture.Error.ToString(), Is.Empty);
        });
    }

    [Test]
    public void Invalid_command_or_option_returns_usage_exit_two_on_stderr()
    {
        var fixture = new CliFixture();

        var unknownCommand = fixture.App.Run(new[] { "unknown" }, fixture.Output, fixture.Error);
        var unknownOption = fixture.App.Run(new[] { "status", "--wat" }, TextWriter.Null, fixture.Error);

        Assert.Multiple(() =>
        {
            Assert.That(unknownCommand, Is.EqualTo(2));
            Assert.That(unknownOption, Is.EqualTo(2));
            Assert.That(fixture.Error.ToString(), Does.Contain("Unknown command 'unknown'"));
            Assert.That(fixture.Error.ToString(), Does.Contain("Unknown option '--wat'"));
        });
    }

    [Test]
    public void Status_uses_the_exact_manifest_PID_and_bearer_token_then_preserves_JSON_output()
    {
        var fixture = new CliFixture();
        fixture.Transport.Response = JsonResponse("{\"apiVersion\":\"1\",\"ok\":true,\"result\":{\"programState\":\"Playing\"}}");

        var exitCode = fixture.App.Run(
            new[] { "status", "--manifest", "session.json", "--pid", "42", "-o", "json" },
            fixture.Output,
            fixture.Error);

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.Zero);
            Assert.That(fixture.Sessions.ManifestPath, Is.EqualTo("session.json"));
            Assert.That(fixture.Sessions.ExpectedProcessId, Is.EqualTo(42));
            Assert.That(fixture.Transport.Request!.Method, Is.EqualTo("GET"));
            Assert.That(fixture.Transport.Request.Uri.AbsoluteUri, Is.EqualTo("http://127.0.0.1:43123/api/v1/status"));
            Assert.That(fixture.Transport.Request.Headers["Authorization"], Is.EqualTo("Bearer test-token"));
            Assert.That(fixture.Output.ToString().Trim(), Is.EqualTo(Encoding.UTF8.GetString(fixture.Transport.Response.Body)));
            Assert.That(fixture.Error.ToString(), Is.Empty);
        });
    }

    [Test]
    public void Screenshot_with_targets_posts_the_typed_crop_request_and_saves_the_png()
    {
        var fixture = new CliFixture();
        var png = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        fixture.Transport.Response = new GatewayClientHttpResponse(200, "image/png", png);

        var exitCode = fixture.App.Run(
            new[]
            {
                "screenshot",
                "--file", "crop.png",
                "--things", "Pawn_42,Building_9",
                "--padding", "24",
                "-o", "json"
            },
            fixture.Output,
            fixture.Error);

        var posted = GatewayContractJson.Read<GatewayScreenshotRequest>(
            Encoding.UTF8.GetString(fixture.Transport.Request!.Body));
        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.Zero);
            Assert.That(fixture.Transport.Request.Method, Is.EqualTo("POST"));
            Assert.That(fixture.Transport.Request.Uri.AbsolutePath, Is.EqualTo("/api/v1/screenshots"));
            Assert.That(posted.ThingHandles, Is.EqualTo(new[] { "Pawn_42", "Building_9" }));
            Assert.That(posted.PaddingPixels, Is.EqualTo(24));
            Assert.That(fixture.Files.BinaryFiles["crop.png"], Is.EqualTo(png));
            Assert.That(fixture.Error.ToString(), Is.Empty);
        });
    }

    [Test]
    public void Execute_source_compiles_before_upload_and_posts_the_compiled_assembly_contract()
    {
        var fixture = new CliFixture();
        fixture.Compiler.Assembly = new byte[] { 1, 2, 3 };
        fixture.Transport.Response = JsonResponse("{\"apiVersion\":\"1\",\"ok\":true}");

        var exitCode = fixture.App.Run(
            new[]
            {
                "execute-source", "snippet.cs",
                "--managed", "managed",
                "--contract", "gateway-contract.dll",
                "--entry-type", "Snippet.Entry",
                "--entry-method", "Run",
                "--request-json", "{\"answer\":42}",
                "-o", "json"
            },
            fixture.Output,
            fixture.Error);

        var postedJson = Encoding.UTF8.GetString(fixture.Transport.Request!.Body);
        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.Zero);
            Assert.That(fixture.Compiler.Request!.SourcePath, Is.EqualTo("snippet.cs"));
            Assert.That(fixture.Compiler.Request.ManagedAssembliesPath, Is.EqualTo("managed"));
            Assert.That(fixture.Compiler.Request.GatewayContractPath, Is.EqualTo("gateway-contract.dll"));
            Assert.That(fixture.Transport.Request.Method, Is.EqualTo("POST"));
            Assert.That(fixture.Transport.Request.Uri.AbsolutePath, Is.EqualTo("/api/v1/executions/assembly"));
            Assert.That(postedJson, Does.Contain("\"assemblyBase64\":\"AQID\""));
            Assert.That(postedJson, Does.Contain("\"entryType\":\"Snippet.Entry\""));
            Assert.That(postedJson, Does.Contain("\"entryMethod\":\"Run\""));
            Assert.That(postedJson, Does.Contain("\\\"answer\\\":42"));
        });
    }

    private static GatewayClientHttpResponse JsonResponse(string json) =>
        new(200, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(json));

    private sealed class CliFixture
    {
        public CliFixture()
        {
            App = new GatewayCliApp(Sessions, Transport, Compiler, Files);
        }

        public RecordingSessionProvider Sessions { get; } = new();

        public RecordingHttpTransport Transport { get; } = new();

        public RecordingCompiler Compiler { get; } = new();

        public RecordingFileSystem Files { get; } = new();

        public StringWriter Output { get; } = new();

        public StringWriter Error { get; } = new();

        public GatewayCliApp App { get; }
    }

    private sealed class RecordingSessionProvider : IGatewaySessionProvider
    {
        public string? ManifestPath { get; private set; }

        public int? ExpectedProcessId { get; private set; }

        public GatewaySessionManifest Load(string? manifestPath, int? expectedProcessId)
        {
            ManifestPath = manifestPath;
            ExpectedProcessId = expectedProcessId;
            return new GatewaySessionManifest
            {
                ApiVersion = "1",
                RunId = "run-test",
                State = "active",
                BaseUrl = "http://127.0.0.1:43123/api/v1",
                Token = "test-token",
                ProcessId = 42,
                ProcessStartUtc = "2026-08-01T10:00:00.0000000+00:00",
                StartedUtc = "2026-08-01T10:00:01.0000000+00:00"
            };
        }
    }

    private sealed class RecordingHttpTransport : IGatewayHttpTransport
    {
        public GatewayClientHttpRequest? Request { get; private set; }

        public GatewayClientHttpResponse Response { get; set; } = JsonResponse("{\"ok\":true}");

        public GatewayClientHttpResponse Send(GatewayClientHttpRequest request)
        {
            Request = request;
            return Response;
        }
    }

    private sealed class RecordingCompiler : IGatewaySourceCompiler
    {
        public GatewaySourceCompilationRequest? Request { get; private set; }

        public byte[] Assembly { get; set; } = new byte[] { 9 };

        public byte[] Compile(GatewaySourceCompilationRequest request)
        {
            Request = request;
            return Assembly;
        }
    }

    private sealed class RecordingFileSystem : IGatewayClientFileSystem
    {
        public Dictionary<string, string> TextFiles { get; } = new();

        public Dictionary<string, byte[]> BinaryFiles { get; } = new();

        public string ReadAllText(string path) => TextFiles[path];

        public void WriteAllBytes(string path, byte[] contents) => BinaryFiles[path] = contents;
    }
}
