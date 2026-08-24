using System.Text.Json;

namespace RimWorldModding.Mcp;

public sealed class GatewayWorkshopClient(
    string repositoryRoot,
    string manifestPath,
    int processId)
{
    private readonly string _repositoryRoot = RepositoryRoot.Resolve(repositoryRoot);
    private readonly string _manifestPath = RepositoryRoot.ContainedPath(repositoryRoot, manifestPath);
    private readonly int _processId = processId > 0 ? processId : throw new ArgumentException("processId must be positive.");

    public async Task RegisterAsync(CancellationToken cancellationToken)
    {
        var source = Path.Combine(_repositoryRoot, "scripts", "Fixtures", "GatewaySteamWorkshopPublisher.cs");
        await RegisterSourceAsync(source, "GatewaySteamWorkshopPublisher.Entry", cancellationToken);
    }

    public async Task RegisterSourceAsync(string source, string entryType, CancellationToken cancellationToken)
    {
        source = RepositoryRoot.ContainedPath(_repositoryRoot, source);
        var managed = @"F:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed";
        var contract = Path.Combine(
            _repositoryRoot, "artifacts", "HostTools", "Release", "net480", "RimWorldDevGateway.Contracts.dll");
        if (!File.Exists(source) || !Directory.Exists(managed) || !File.Exists(contract))
            throw new InvalidOperationException("Workshop publisher source, RimWorld managed directory, or Gateway contract is missing.");
        var outer = await CallAsync(
            [
                "execute-source", source,
                "--managed", managed,
                "--contract", contract,
                "--entry-type", entryType,
                "--entry-method", "Execute",
                "--request-json", "{}"
            ],
            cancellationToken);
        RequireOk(outer, "Workshop publisher registration");
    }

    public async Task<JsonElement> InvokeAsync(
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        return await InvokeAutomationAsync("release.workshop", arguments, cancellationToken);
    }

    public async Task<JsonElement> InvokeAutomationAsync(
        string name,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        var argumentsJson = JsonSerializer.Serialize(arguments);
        var outer = await CallAsync(
            ["run", name, "--arguments", argumentsJson],
            cancellationToken);
        RequireOk(outer, "Workshop automation dispatch");
        if (!outer.TryGetProperty("result", out var result) ||
            !TryProperty(result, "State", out var state) || state.GetString() != "succeeded" ||
            !TryProperty(result, "Result", out var resultJson) || resultJson.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException("Workshop automation did not return one succeeded result.");
        using var snapshot = JsonDocument.Parse(resultJson.GetString()!);
        return snapshot.RootElement.Clone();
    }

    public async Task CaptureScreenshotAsync(string outputPath, CancellationToken cancellationToken)
    {
        var path = RepositoryRoot.ContainedPath(_repositoryRoot, outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var outer = await CallAsync(["screenshot", "--file", path], cancellationToken);
        RequireOk(outer, "Gateway screenshot");
        if (!File.Exists(path) || new FileInfo(path).Length == 0)
            throw new InvalidOperationException("Gateway screenshot did not produce a nonempty file.");
    }

    public async Task<JsonElement> ExecuteSourceAsync(
        string sourcePath,
        string entryType,
        string requestJson,
        CancellationToken cancellationToken)
    {
        var source = RepositoryRoot.ContainedPath(_repositoryRoot, sourcePath);
        if (!File.Exists(source)) throw new ArgumentException("Gateway source file does not exist.");
        if (string.IsNullOrWhiteSpace(entryType) || entryType.Length > 256)
            throw new ArgumentException("entryType is invalid.");
        using (var request = JsonDocument.Parse(requestJson))
        {
            if (request.RootElement.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("requestJson must be one JSON object.");
        }
        var managed = @"F:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed";
        var contract = Path.Combine(
            _repositoryRoot, "artifacts", "HostTools", "Release", "net480", "RimWorldDevGateway.Contracts.dll");
        var outer = await CallAsync(
            [
                "execute-source", source,
                "--managed", managed,
                "--contract", contract,
                "--entry-type", entryType,
                "--entry-method", "Execute",
                "--request-json", requestJson
            ],
            cancellationToken);
        RequireOk(outer, "Raw Gateway source execution");
        return outer;
    }

    public async Task<JsonElement> WaitTerminalAsync(
        string planSha256,
        IReadOnlySet<string> terminalStatuses,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        JsonElement last = default;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            last = await InvokeAsync(new Dictionary<string, object?> { ["operation"] = "status" }, cancellationToken);
            var statusPlan = String(last, "PlanSha256");
            if (statusPlan.Length > 0 && !string.Equals(statusPlan, planSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Workshop status belongs to a different publication plan.");
            if (terminalStatuses.Contains(String(last, "Status"))) return last;
            await Task.Delay(500, cancellationToken);
        }
        throw new TimeoutException($"Workshop operation did not finish; last stage was '{String(last, "Stage")}'.");
    }

    public static string String(JsonElement element, string name)
    {
        if (!TryProperty(element, name, out var property)) return "";
        return property.ValueKind == JsonValueKind.String ? property.GetString() ?? "" : property.ToString();
    }

    public static ulong UInt64(JsonElement element, string name)
    {
        if (!TryProperty(element, name, out var property)) return 0;
        return property.TryGetUInt64(out var value) ? value : ulong.TryParse(property.ToString(), out value) ? value : 0;
    }

    public static bool TryProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }
        value = default;
        return false;
    }

    private async Task<JsonElement> CallAsync(IReadOnlyList<string> commandArguments, CancellationToken cancellationToken)
    {
        var client = Path.Combine(
            _repositoryRoot, "artifacts", "HostTools", "Release", "net480", "RimWorldDevGateway.Client.exe");
        if (!File.Exists(client)) throw new InvalidOperationException("Built Gateway companion client is missing.");
        var arguments = commandArguments.ToList();
        arguments.Add("--manifest");
        arguments.Add(_manifestPath);
        arguments.Add("--pid");
        arguments.Add(_processId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        arguments.Add("--output");
        arguments.Add("json");
        var result = await ProcessRunner.RunAsync(
            client, arguments, _repositoryRoot, TimeSpan.FromMinutes(3), cancellationToken);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Gateway client failed: {Bound(result.StandardError + result.StandardOutput)}");
        try
        {
            using var document = JsonDocument.Parse(result.StandardOutput.Trim());
            return document.RootElement.Clone();
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Gateway client returned invalid JSON: {exception.Message}");
        }
    }

    private static void RequireOk(JsonElement outer, string operation)
    {
        if (!outer.TryGetProperty("ok", out var ok) || ok.ValueKind != JsonValueKind.True)
            throw new InvalidOperationException($"{operation} failed: {Bound(outer.ToString())}");
    }

    private static string Bound(string value) => value.Length <= 8192 ? value.Trim() : value[..8192].Trim();
}
