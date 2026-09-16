using System.Globalization;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;

namespace RimWorldModding.Mcp;

public static class GatewayLogArguments
{
    public static string[] Create(long after, int limit)
    {
        if (after < 0) throw new ArgumentException("after must be nonnegative.");
        if (limit is < 1 or > 500) throw new ArgumentException("limit must be between 1 and 500.");
        return ["logs", "--after", after.ToString(CultureInfo.InvariantCulture),
            "--limit", limit.ToString(CultureInfo.InvariantCulture)];
    }
}

public sealed record GatewayDiagnosticResult(string RunId, int GameProcessId,
    DateTimeOffset GameProcessStartUtc, JsonElement Response);

public sealed class GatewayDiagnostics(string repositoryRoot)
{
    public static CancellationTokenSource CreateDeadline(CancellationToken caller, TimeSpan? timeout = null)
    {
        var deadline = CancellationTokenSource.CreateLinkedTokenSource(caller);
        deadline.CancelAfter(timeout ?? TimeSpan.FromSeconds(180));
        return deadline;
    }

    public async Task<GatewayDiagnosticResult> InspectAsync(string runId, string name,
        IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var status = Ready(runId);
        var client = new GatewayWorkshopClient(repositoryRoot, status.GatewayManifestPath!, status.GameProcessId!.Value);
        return Result(status, await client.InspectDiagnosticsAsync(name, arguments, cancellationToken));
    }

    public async Task<GatewayDiagnosticResult> DecompileAsync(string runId, string methodHandle, bool merged,
        CancellationToken cancellationToken)
    {
        using var deadline = CreateDeadline(cancellationToken);
        cancellationToken = deadline.Token;
        var status = Ready(runId);
        var inspected = await InspectAsync(runId, merged ? "diagnostics.merged" : "diagnostics.method",
            new Dictionary<string, object?> { ["methodHandle"] = methodHandle }, cancellationToken);
        var payload = Payload(inspected.Response);
        var method = merged ? Property(payload, "Method") : payload;
        byte[] bytes;
        Guid? expectedMvid = null;
        var assemblyPath = Property(method, "AssemblyPath").GetString()!;
        var token = Property(merged ? payload : method, "MetadataToken").GetInt32();
        if (merged)
        {
            bytes = Convert.FromBase64String(Property(payload, "AssemblyBase64").GetString()!);
        }
        else
        {
            if (!Path.IsPathFullyQualified(assemblyPath) || !File.Exists(assemblyPath))
                throw new ArgumentException("This method has no accessible original assembly file; byte-loaded methods require retained original bytes.");
            await using var input = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (input.Length > GatewayMethodDecompiler.MaximumAssemblyBytes)
                throw new ArgumentException("Original assembly exceeds the 64 MiB input policy bound.");
            bytes = new byte[checked((int)input.Length)];
            await input.ReadExactlyAsync(bytes, cancellationToken);
            expectedMvid = Guid.Parse(Property(method, "ModuleMvid").GetString()!);
        }
        var result = GatewayMethodDecompiler.Decompile(bytes, expectedMvid, token, assemblyPath, cancellationToken);
        var directory = Path.Combine(status.RunRoot, "diagnostics");
        Directory.CreateDirectory(directory);
        var stem = Path.Combine(directory, $"method-{Guid.NewGuid():N}");
        await File.WriteAllBytesAsync(stem + ".dll", bytes, cancellationToken);
        await File.WriteAllTextAsync(stem + ".cs", result.Code, cancellationToken);
        var response = JsonSerializer.SerializeToElement(new { mode = merged ? "reconstructed-current" : "original",
            method, result.Code, result.InputSha256, result.ModuleMvid, result.MetadataToken,
            sourcePath = stem + ".cs", assemblyPath = stem + ".dll", request = inspected.Response,
            historicalCrashLine = (int?)null });
        // Avoid duplicating the large merged byte payload in the retained provenance.
        if (merged) response = JsonSerializer.SerializeToElement(new { mode = "reconstructed-current", method,
            result.Code, result.InputSha256, result.ModuleMvid, result.MetadataToken,
            sourcePath = stem + ".cs", assemblyPath = stem + ".dll",
            requestId = Property(inspected.Response, "requestId"), historicalCrashLine = (int?)null });
        var output = Result(status, response);
        await File.WriteAllTextAsync(stem + ".json", OperationJson.Serialize(output), cancellationToken);
        return output;
    }

    public async Task<GatewayDiagnosticResult> ReportAsync(string runId, string errorId, CancellationToken cancellationToken)
    {
        var status = Ready(runId);
        var inspected = await InspectAsync(runId, "diagnostics.errors",
            new Dictionary<string, object?> { ["id"] = errorId }, cancellationToken);
        var payload = Payload(inspected.Response);
        var error = Property(payload, "Error");
        var text = new StringBuilder();
        text.AppendLine("# RimWorld Gateway error report").AppendLine()
            .AppendLine($"Run: {runId}; process: {status.GameProcessId}; started: {status.GameProcessStartUtc:O}")
            .AppendLine($"Error: {Property(error, "Id")}; occurrences: {Property(error, "Occurrences")}")
            .AppendLine($"First: {Property(error, "FirstSeenUtc")}; last: {Property(error, "LastSeenUtc")}")
            .AppendLine().AppendLine(Property(error, "Message").GetString())
            .AppendLine().AppendLine("The accompanying JSON contains the exact captured frames, nested causes and current patch attribution.")
            .AppendLine("Patch attribution describes involvement, not proof of fault. No historical crash-line mapping is claimed.")
            .AppendLine().AppendLine("````text").AppendLine(Property(error, "Stack").GetString()).AppendLine("````")
            .AppendLine().AppendLine("````json").AppendLine(payload.ToString()).AppendLine("````");
        var directory = Path.Combine(status.RunRoot, "diagnostics");
        Directory.CreateDirectory(directory);
        var stem = Path.Combine(directory, $"error-{Guid.NewGuid():N}");
        var markdown = text.ToString();
        await File.WriteAllTextAsync(stem + ".md", markdown, cancellationToken);
        await File.WriteAllTextAsync(stem + ".json", OperationJson.Serialize(inspected), cancellationToken);
        return Result(status, JsonSerializer.SerializeToElement(new { markdownPath = stem + ".md", jsonPath = stem + ".json",
            sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(markdown))), request = inspected.Response }));
    }

    internal static JsonElement Payload(JsonElement outer)
    {
        var run = Property(outer, "result");
        if (Property(run, "State").GetString() != "succeeded")
            throw new InvalidOperationException("Gateway diagnostics failed: " + run.ToString());
        return Property(run, "Result");
    }

    private static JsonElement Property(JsonElement value, string name) =>
        GatewayWorkshopClient.TryProperty(value, name, out var property) ? property :
            throw new InvalidOperationException("Gateway diagnostic response is missing " + name + ".");

    public async Task<GatewayDiagnosticResult> LogsAsync(string runId, long after, int limit,
        CancellationToken cancellationToken)
    {
        _ = GatewayLogArguments.Create(after, limit);
        var status = Ready(runId);
        var client = new GatewayWorkshopClient(repositoryRoot, status.GatewayManifestPath!, status.GameProcessId!.Value);
        return Result(status, await client.ReadLogsAsync(after, limit, cancellationToken));
    }

    private RunStatusResult Ready(string runId)
    {
        var status = new RunLeaseManager(repositoryRoot).Status(runId);
        if (status.State != "ready" || status.GameProcessId is null ||
            status.GameProcessStartUtc is null || status.GatewayManifestPath is null)
            throw new ArgumentException("The selected run has no exact ready Gateway process and manifest.");
        return status;
    }

    private static GatewayDiagnosticResult Result(RunStatusResult status, JsonElement response) =>
        new(status.RunId, status.GameProcessId!.Value, status.GameProcessStartUtc!.Value, response);
}
