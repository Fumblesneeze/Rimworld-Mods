using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

// Shared real-companion host fixture. It never connects to a game process.
internal static class GatewayCompanionFixture
{
    internal static async Task<(int ExitCode, string Output, string Error, string? RequestLine, string? Body)> InvokeCommand(
        string[] commandArguments, string outputMode, int status, string response,
        string? typedOperation = null, object? typedArguments = null)
    {
        var root = TestRepository.FindRoot();
        var runId = RunLeaseManager.ReserveRunId();
        var directory = typedOperation is null
            ? Path.Combine(root, "artifacts", "TestFixtures", "automation-client-" + Guid.NewGuid().ToString("N"))
            : Path.Combine(root, "artifacts", "McpRuns", runId);
        Directory.CreateDirectory(directory);
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var current = Process.GetCurrentProcess();
        var processStart = new DateTimeOffset(current.StartTime.ToUniversalTime(), TimeSpan.Zero);
        var manifest = Path.Combine(directory, "DevGateway", "current.json");
        Directory.CreateDirectory(Path.GetDirectoryName(manifest)!);
        await File.WriteAllTextAsync(manifest, JsonSerializer.Serialize(new
        {
            apiVersion = "1", state = "active", token = "synthetic-test-token",
            baseUrl = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/api/v1",
            processId = current.Id,
            processStartUtc = processStart.ToString("O")
        }));
        if (typedOperation is not null)
        {
            // Lease the test host only. This exercises process identity/dispatch, never a game fixture.
            var gameLease = Path.Combine(directory, "game-process.json");
            await File.WriteAllTextAsync(gameLease, JsonSerializer.Serialize(new { processId = current.Id, processStartUtc = processStart }));
            var lease = new RunLeaseRecord("RimWorldModdingMcp/RunLease/v1", runId, "synthetic-test-host", current.Id,
                processStart, directory, Path.Combine(directory, "complete.signal"), gameLease,
                Path.Combine(directory, "stdout.log"), Path.Combine(directory, "stderr.log"), DateTimeOffset.UtcNow);
            await File.WriteAllTextAsync(Path.Combine(directory, "mcp-run.json"), JsonSerializer.Serialize(lease));
        }
        var configuration = typeof(GatewayCompanionFixture).Assembly.Location.Contains("Debug") ? "Debug" : "Release";
        var start = new ProcessStartInfo(Path.Combine(root, "artifacts", "HostTools", configuration, "net480", "RimWorldDevGateway.Client.exe"))
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var argument in commandArguments.Concat(new[] { "--manifest", manifest,
                     "--pid", current.Id.ToString(), "-o", outputMode })) start.ArgumentList.Add(argument);
        using var child = new Process { StartInfo = start };
        var started = false;
        try
        {
            using var typedOutput = new StringWriter();
            using var typedError = new StringWriter();
            Task<string> stdout;
            Task<string> stderr;
            Task<int> exited;
            if (typedOperation is null)
            {
                started = child.Start();
                stdout = child.StandardOutput.ReadToEndAsync(deadline.Token);
                stderr = child.StandardError.ReadToEndAsync(deadline.Token);
                exited = Task.Run(async () => { await child.WaitForExitAsync(deadline.Token); return child.ExitCode; });
            }
            else
            {
                var arguments = JsonSerializer.SerializeToElement(typedArguments).EnumerateObject()
                    .ToDictionary(property => property.Name, property => property.Value.Clone());
                arguments.Add("runId", JsonSerializer.SerializeToElement(runId));
                exited = Task.Run(() => McpCli.InvokeAsync(new[] { "tool", "call", typedOperation, "--repository-root", root,
                    "--arguments", JsonSerializer.Serialize(arguments), "-o", outputMode }, typedOutput, typedError, deadline.Token));
                stdout = Task.FromResult("");
                stderr = Task.FromResult("");
            }
            var accepted = listener.AcceptTcpClientAsync(deadline.Token).AsTask();
            string? requestLine = null;
            string? body = null;
            if (await Task.WhenAny(accepted, exited) == accepted)
            {
                using var socket = await accepted;
                using var stream = socket.GetStream();
                using var reader = new StreamReader(stream, Encoding.Latin1, false, 1024, leaveOpen: true);
                requestLine = await reader.ReadLineAsync(deadline.Token);
                var headers = new List<string>();
                string? line;
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(deadline.Token))) headers.Add(line);
                Assert.That(headers, Does.Contain("Authorization: Bearer synthetic-test-token"));
                var lengthHeader = headers.SingleOrDefault(value => value.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase));
                var length = lengthHeader is null ? 0 : int.Parse(lengthHeader.Split(':')[1]);
                Assert.That(length, Is.InRange(0, 65536));
                var bodyText = new StringBuilder();
                // Latin1 keeps one character per HTTP byte; decode the complete body as UTF-8 below.
                for (var index = 0; index < length; index++)
                {
                    var next = new char[1];
                    Assert.That(await reader.ReadAsync(next.AsMemory(), deadline.Token), Is.EqualTo(1));
                    bodyText.Append(next[0]);
                }
                body = Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(bodyText.ToString()));
                var bytes = Encoding.UTF8.GetBytes(response);
                var head = Encoding.ASCII.GetBytes($"HTTP/1.1 {status} Test\r\nContent-Type: application/json\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(head, deadline.Token);
                await stream.WriteAsync(bytes, deadline.Token);
            }
            var exitCode = await exited;
            return (exitCode, typedOperation is null ? await stdout : typedOutput.ToString(),
                typedOperation is null ? await stderr : typedError.ToString(), requestLine, body);
        }
        finally
        {
            deadline.Cancel();
            listener.Stop();
            if (started && !child.HasExited) { child.Kill(); child.WaitForExit(2000); }
            Directory.Delete(directory, recursive: true);
        }
    }
}
