using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
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
using Verse;

public static class TestSnippet
{
    public static string Execute(string requestJson)
    {
        return typeof(Pawn).Name + new GatewaySessionManifest().ApiVersion + requestJson;
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
}
