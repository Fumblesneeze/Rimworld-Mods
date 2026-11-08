using System.Text.Json;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class GatewayDebugActionClientTests
{
    [TestCase("json")]
    [TestCase("table")]
    public async Task CompanionEmitsMultilingualNativeResponsesAsUtf8(string format)
    {
        const string label = "Größe — 温度 — Политика — 🐾";
        var result = await GatewayCompanionFixture.InvokeCommand(new[] { "debug-actions", "query" }, format, 200,
            "{\"ok\":true,\"result\":{\"Label\":\"" + label + "\"}}");
        Assert.That(result.ExitCode, Is.Zero, result.Error);
        Assert.That(result.Output, Does.Contain(label));
    }

    [Test]
    public async Task TypedCliRetainsMultilingualNativeResponseThroughTheCompanionProcess()
    {
        const string label = "Größe — 温度 — Политика — 🐾";
        var result = await GatewayCompanionFixture.InvokeCommand(Array.Empty<string>(), "json", 200,
            "{\"ok\":true,\"result\":{\"Items\":[{\"Label\":\"" + label + "\",\"Handle\":\"debug-1\"}]}}",
            "gateway_debug_actions", new { search = "native" });
        Assert.That(result.ExitCode, Is.Zero, result.Error);
        using var operation = JsonDocument.Parse(result.Output);
        using var native = JsonDocument.Parse(operation.RootElement.GetProperty("standardOutput").GetString()!);
        Assert.That(native.RootElement.GetProperty("result").GetProperty("Items")[0].GetProperty("Label").GetString(), Is.EqualTo(label));
    }

    [Test]
    public async Task TypedCliKeepsNativeRefusalsAsRuntimeFailures()
    {
        var result = await GatewayCompanionFixture.InvokeCommand(Array.Empty<string>(), "json", 400,
            "{\"ok\":false,\"error\":{\"code\":\"stale_debug_action_handle\",\"message\":\"温度 — Größe\"}}",
            "gateway_debug_action_invoke", new { handle = "--help" });
        Assert.That(result.ExitCode, Is.EqualTo(1), result.Output + result.Error);
        Assert.That(result.RequestLine, Is.EqualTo("POST /api/v1/dev-tools/actions/--help/invoke HTTP/1.1"));
        Assert.That(result.Error, Does.Contain("stale_debug_action_handle"));
        Assert.That(result.Error, Does.Contain("温度 — Größe"));
    }

    [Test]
    public async Task TypedCliPreservesCompanionUsageClassification()
    {
        var result = await GatewayCompanionFixture.InvokeCommand(Array.Empty<string>(), "json", 200, "{}",
            "gateway_debug_actions", new { limit = 0 });
        Assert.That(result.ExitCode, Is.EqualTo(2), result.Output + result.Error);
        Assert.That(result.RequestLine, Is.Null);
        Assert.That(result.Error, Does.Contain("limit"));
    }

    [TestCase("--help")]
    [TestCase("-h")]
    [TestCase("--pid")]
    [TestCase("--manifest")]
    [TestCase("-o")]
    public async Task InvokeTreatsTheExactHandleAsPositionalData(string handle)
    {
        var result = await GatewayCompanionFixture.InvokeCommand(new[] { "debug-actions", "invoke", handle },
            "json", 400, "{\"ok\":false,\"error\":{\"code\":\"stale_debug_action_handle\"}}");
        Assert.That(result.ExitCode, Is.EqualTo(1), result.Output + result.Error);
        Assert.That(result.RequestLine, Is.EqualTo("POST /api/v1/dev-tools/actions/" + handle + "/invoke HTTP/1.1"));
        Assert.That(result.Error, Does.Contain("stale_debug_action_handle"));
        Assert.That(result.Output, Is.Empty);
    }

    [TestCase(65536, true)]
    [TestCase(65537, false)]
    public async Task ExactBoundAndExistingDefaultRetainTheirPreviousOutputContract(int length, bool complete)
    {
        var command = new AdapterCommand("output-fixture", "pwsh",
            new[] { "-NoProfile", "-NonInteractive", "-Command", "[Console]::Out.Write('x' * " + length + ")" },
            TestRepository.FindRoot(), TimeSpan.FromSeconds(15));
        var response = complete
            ? await RepositoryOperations.ExecuteAsync(command, CancellationToken.None, requireCompleteOutput: true)
            : await RepositoryOperations.ExecuteAsync(command, CancellationToken.None);
        Assert.That(response.ExitCode, Is.Zero);
        Assert.That(response.StandardOutput.Length, Is.EqualTo(65536));
    }

    [Test]
    public void CompleteResponseRequirementRejectsOversizeActualChildOutput()
    {
        var command = new AdapterCommand("gateway_debug_actions", "pwsh",
            new[] { "-NoProfile", "-NonInteractive", "-Command", "[Console]::Out.Write('x' * 65537)" },
            TestRepository.FindRoot(), TimeSpan.FromSeconds(15));
        var error = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await RepositoryOperations.ExecuteAsync(command, CancellationToken.None, requireCompleteOutput: true));
        Assert.That(error!.Message, Does.Contain("65536").And.Contain("limit"));
    }

    [TestCase("--help")]
    [TestCase("--pid")]
    [TestCase("--manifest")]
    public async Task QueryOptionValuesAreDataEvenWhenTheyLookLikeCliOptions(string search)
    {
        var result = await GatewayCompanionFixture.InvokeCommand(new[] { "debug-actions", "query", "--search", search },
            "json", 200, "{\"ok\":true}");
        Assert.That(result.ExitCode, Is.Zero, result.Error);
        Assert.That(result.RequestLine, Is.EqualTo("POST /api/v1/dev-tools/actions/query HTTP/1.1"));
        using var body = JsonDocument.Parse(result.Body!);
        Assert.That(body.RootElement.GetProperty("search").GetString(), Is.EqualTo(search));
    }

    [Test]
    public async Task PlannerBindsSessionAndTransportsExactUnicodeFilterAndCursor()
    {
        var root = RunLeaseAndGatewayPlannerTests.TestRoot();
        try
        {
            var manifest = Path.Combine(root, "artifacts", "run", "current.json");
            Directory.CreateDirectory(Path.GetDirectoryName(manifest)!);
            File.WriteAllText(manifest, "{}");
            var query = GatewayClientPlanner.DebugActions(root, manifest, 123, "  reload \"温度\"  ",
                new[] { " General " }, new[] { "immediate" }, new[] { "Playing" }, " cursor ", 1000);
            Assert.That(query.Kind, Is.EqualTo("gateway_debug_actions"));
            Assert.That(query.Arguments.TakeLast(6), Is.EqualTo(new[] { "--manifest", manifest, "--pid", "123", "--output", "json" }));
            var command = query.Arguments.SkipWhile(value => value != "--").Skip(1).SkipLast(6).ToArray();
            var response = await GatewayCompanionFixture.InvokeCommand(command, "json", 200, "{\"ok\":true,\"result\":{}}");
            Assert.That(response.ExitCode, Is.Zero, response.Error);
            using var body = JsonDocument.Parse(response.Body!);
            Assert.That(body.RootElement.GetProperty("search").GetString(), Is.EqualTo("  reload \"温度\"  "));
            Assert.That(body.RootElement.GetProperty("categories")[0].GetString(), Is.EqualTo(" General "));
            Assert.That(body.RootElement.GetProperty("after").GetString(), Is.EqualTo(" cursor "));
            Assert.That(body.RootElement.GetProperty("limit").GetInt32(), Is.EqualTo(1000));

            var invocation = GatewayClientPlanner.DebugActionInvoke(root, manifest, 123, "debug-abc_123");
            Assert.That(invocation.Kind, Is.EqualTo("gateway_debug_action_invoke"));
            Assert.That(invocation.Arguments.TakeLast(6), Is.EqualTo(query.Arguments.TakeLast(6)));
            command = invocation.Arguments.SkipWhile(value => value != "--").Skip(1).SkipLast(6).ToArray();
            response = await GatewayCompanionFixture.InvokeCommand(command, "table", 200, "{\"ok\":true}");
            Assert.That(response.ExitCode, Is.Zero, response.Error);
            Assert.That(response.RequestLine, Is.EqualTo("POST /api/v1/dev-tools/actions/debug-abc_123/invoke HTTP/1.1"));
            Assert.Throws<ArgumentException>(() => GatewayClientPlanner.DebugActionInvoke(root, manifest, 123, ".."));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Test]
    public async Task HelpExplainsQueryAndNativePointerInvocation()
    {
        var result = await GatewayCompanionFixture.InvokeCommand(new[] { "--help" }, "table", 200, "{}");
        Assert.That(result.ExitCode, Is.Zero);
        Assert.That(result.Output, Does.Contain("debug-actions query").And.Contain("debug-actions invoke").And.Contain("pointer"));
        Assert.That(result.RequestLine, Is.Null);
    }

    [Test]
    public async Task NativeRefusalIsRetainedWithoutRetry()
    {
        var result = await GatewayCompanionFixture.InvokeCommand(new[] { "debug-actions", "invoke", "expired" },
            "json", 400, "{\"ok\":false,\"error\":{\"code\":\"stale_debug_action_handle\"}}");
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Error, Does.Contain("stale_debug_action_handle"));
        Assert.That(result.Output, Is.Empty);
    }

    [TestCaseSource(nameof(InvalidArguments))]
    public async Task InvalidArgumentsRejectBeforeTransport(string[] arguments)
    {
        var result = await GatewayCompanionFixture.InvokeCommand(arguments, "json", 200, "{}");
        Assert.That(result.ExitCode, Is.EqualTo(2), result.Error);
        Assert.That(result.RequestLine, Is.Null);
    }

    private static IEnumerable<string[]> InvalidArguments()
    {
        yield return new[] { "debug-actions" };
        yield return new[] { "debug-actions", "invoke", "bad/handle" };
        yield return new[] { "debug-actions", "invoke", ".." };
        yield return new[] { "debug-actions", "invoke", "." };
        yield return new[] { "debug-actions", "invoke", "bad\\handle" };
        yield return new[] { "debug-actions", "invoke", "handle\n" };
        yield return new[] { "debug-actions", "invoke", " " };
        yield return new[] { "debug-actions", "query", "--limit", "0" };
        yield return new[] { "debug-actions", "query", "--limit", "1001" };
        yield return new[] { "debug-actions", "query", "--search", new string('x', 257) };
        yield return new[] { "debug-actions", "query", "--categories-json", "[1]" };
        yield return new[] { "debug-actions", "query", "--categories-json", "null" };
        yield return new[] { "debug-actions", "query", "--categories-json", "{" };
        yield return new[] { "debug-actions", "query", "--categories-json", JsonSerializer.Serialize(Enumerable.Range(0,65).Select(i => i.ToString())) };
        yield return new[] { "debug-actions", "query", "--allowed-game-states-json", JsonSerializer.Serialize(Enumerable.Range(0,65).Select(i => i.ToString())) };
        yield return new[] { "debug-actions", "query", "--modes-json", "[\"unknown\"]" };
        yield return new[] { "debug-actions", "query", "--limit", "1", "--limit", "2" };
        yield return new[] { "debug-actions", "query", "--unknown", "x" };
    }

    [TestCase("gateway_debug_actions", OperationRisk.Read)]
    [TestCase("gateway_debug_action_invoke", OperationRisk.DestructiveLocal)]
    public void TypedOperationRequiresItsExactReadyRun(string name, OperationRisk risk)
    {
        var registry = OperationRegistry.CreateDefault(TestRepository.FindRoot());
        Assert.That(registry.Descriptors.SingleOrDefault(item => item.Name == name)?.Risk, Is.EqualTo(risk));
        var error = Assert.ThrowsAsync<ArgumentException>(async () => await registry.InvokeAsync(name,
            JsonSerializer.SerializeToElement(new { runId = "missing-debug-action-run", handle = "action-1" }), CancellationToken.None));
        Assert.That(error!.Message, Does.Contain("run").IgnoreCase);
    }

    [TestCase("json", "Immediate")]
    [TestCase("table", "MapPointer")]
    public async Task InvokeUsesOnlyExactHandleAndPreservesNativeMode(string format, string mode)
    {
        var result = await GatewayCompanionFixture.InvokeCommand(new[] { "debug-actions", "invoke", "action-1" },
            format, 200, "{\"ok\":true,\"requestId\":\"invoke-1\",\"result\":{\"Mode\":\"" + mode + "\",\"Handle\":\"action-1\"}}");
        Assert.That(result.ExitCode, Is.Zero, result.Error);
        Assert.That(result.RequestLine, Is.EqualTo("POST /api/v1/dev-tools/actions/action-1/invoke HTTP/1.1"));
        Assert.That(result.Body, Is.EqualTo("{}"));
        Assert.That(result.Output, Does.Contain(mode).And.Contain("invoke-1"));
        if (format == "table") Assert.That(result.Output, Does.Contain("FIELD\tVALUE"));
    }

    [Test]
    public async Task QueryPreservesExactFiltersCursorAndReturnedNativeHandle()
    {
        var result = await GatewayCompanionFixture.InvokeCommand(new[] {
            "debug-actions", "query", "--search", "Hot reload", "--categories-json", "[\"General, tools\"]",
            "--modes-json", "[\"Immediate\"]", "--allowed-game-states-json", "[\"Playing\"]",
            "--after", "opaque:cursor", "--limit", "7" }, "json", 200,
            "{\"ok\":true,\"requestId\":\"query-1\",\"result\":{\"Items\":[{\"Handle\":\"action-1\"}],\"NextCursor\":\"next\"}}");
        Assert.That(result.ExitCode, Is.Zero, result.Error);
        Assert.That(result.RequestLine, Is.EqualTo("POST /api/v1/dev-tools/actions/query HTTP/1.1"));
        using var body = JsonDocument.Parse(result.Body!);
        Assert.That(body.RootElement.GetProperty("search").GetString(), Is.EqualTo("Hot reload"));
        Assert.That(body.RootElement.GetProperty("categories")[0].GetString(), Is.EqualTo("General, tools"));
        Assert.That(body.RootElement.GetProperty("modes")[0].GetString(), Is.EqualTo("Immediate"));
        Assert.That(body.RootElement.GetProperty("allowedGameStates")[0].GetString(), Is.EqualTo("Playing"));
        Assert.That(body.RootElement.GetProperty("after").GetString(), Is.EqualTo("opaque:cursor"));
        Assert.That(body.RootElement.GetProperty("limit").GetInt32(), Is.EqualTo(7));
        Assert.That(result.Output, Does.Contain("action-1").And.Contain("query-1").And.Contain("next"));
        Assert.That(result.Error, Is.Empty);
    }
}
