using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

// Exercise the actual .NET Framework companion as a child process. The synthetic loopback server
// and manifest represent only this host test process; no game, live session or real token is used.
[TestFixture]
public sealed class GatewayAutomationClientTests
{
    [Test]
    public async Task StatusUsesTheRetainedRunReadRouteAndPreservesJson()
    {
        var result = await Invoke("status", "json", "gesture-1", 200,
            "{\"ok\":true,\"result\":{\"RunId\":\"gesture-1\",\"State\":\"running\"}}");
        Assert.That(result.ExitCode, Is.Zero, result.Error);
        Assert.That(result.RequestLine, Is.EqualTo("GET /api/v1/automation-runs/gesture-1 HTTP/1.1"));
        Assert.That(result.Output, Does.Contain("\"State\":\"running\""));
        Assert.That(result.Error, Is.Empty);
    }

    [Test]
    public async Task CancelPostsToTheExistingRunAndCanRenderTheAcknowledgementAsATable()
    {
        var result = await Invoke("cancel", "table", "gesture-1", 200,
            "{\"ok\":true,\"result\":{\"cancellationRequested\":true,\"automationRunId\":\"gesture-1\"}}");
        Assert.That(result.ExitCode, Is.Zero, result.Error);
        Assert.That(result.RequestLine, Is.EqualTo("POST /api/v1/automation-runs/gesture-1/cancel HTTP/1.1"));
        Assert.That(result.Output, Does.Contain("FIELD\tVALUE"));
        Assert.That(result.Output, Does.Contain("gesture-1"));
        Assert.That(result.Error, Is.Empty);
    }

    [TestCase("bad/id")]
    [TestCase("gesture-1\n")]
    public async Task InvalidRunIdentityIsRejectedBeforeTransport(string id)
    {
        var result = await Invoke("status", "json", id, 200, "{\"ok\":true,\"result\":{}}");
        Assert.That(result.ExitCode, Is.EqualTo(2));
        Assert.That(result.RequestLine, Is.Null);
    }

    [Test]
    public async Task MissingRunKeepsTheServerErrorAndReturnsRuntimeFailure()
    {
        var result = await Invoke("status", "json", "expired-run", 400,
            "{\"ok\":false,\"error\":{\"code\":\"automation_run_not_found\"}}");
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Error, Does.Contain("automation_run_not_found"));
        Assert.That(result.Output, Is.Empty);
    }

    private static Task<(int ExitCode, string Output, string Error, string? RequestLine, string? Body)> Invoke(
        string command, string outputMode, string automationId, int status, string response)
        => GatewayCompanionFixture.InvokeCommand(new[] { "automations", command, automationId }, outputMode, status, response);

}
