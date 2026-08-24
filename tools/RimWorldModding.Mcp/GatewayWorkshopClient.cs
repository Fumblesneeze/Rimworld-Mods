using System.Security.Cryptography;
using System.Text;
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
        var companion = await EnsureCompanionAsync(cancellationToken);
        var managed = @"F:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed";
        if (!File.Exists(source) || !Directory.Exists(managed))
            throw new InvalidOperationException("Workshop publisher source, RimWorld managed directory, or Gateway contract is missing.");
        var outer = await CallAsync(
            [
                "execute-source", source,
                "--managed", managed,
                "--contract", companion.ContractPath,
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
        var companion = await EnsureCompanionAsync(cancellationToken);
        var outer = await CallAsync(
            [
                "execute-source", source,
                "--managed", managed,
                "--contract", companion.ContractPath,
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
        var companion = await EnsureCompanionAsync(cancellationToken);
        var arguments = commandArguments.ToList();
        arguments.Add("--manifest");
        arguments.Add(_manifestPath);
        arguments.Add("--pid");
        arguments.Add(_processId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        arguments.Add("--output");
        arguments.Add("json");
        var result = await ProcessRunner.RunAsync(
            companion.ClientPath, arguments, _repositoryRoot, TimeSpan.FromMinutes(3), cancellationToken);
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

    internal static GatewayCompanionBuildPlan CreateCompanionBuildPlan(string repositoryRoot)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        var project = Path.Combine(root, "tools", "RimWorldDevGateway.Client", "RimWorldDevGateway.Client.csproj");
        var inputs = new[]
            {
                Path.Combine(root, "Directory.Build.props"),
                Path.Combine(root, "Directory.Build.targets"),
                Path.Combine(root, "global.json")
            }
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "tools", "RimWorldDevGateway.Client"), "*", SearchOption.AllDirectories))
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "shared", "RimWorldDevGateway.Contracts"), "*", SearchOption.AllDirectories))
            .Where(path => !Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                               .Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                                            part.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".props", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".targets", StringComparison.OrdinalIgnoreCase) ||
                           Path.GetFileName(path).Equals("global.json", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => Path.GetRelativePath(root, path), StringComparer.Ordinal)
            .ToArray();
        if (!File.Exists(project) || inputs.Any(path => !File.Exists(path)))
            throw new InvalidOperationException("Gateway companion build inputs are missing.");
        var fingerprint = Fingerprint(root, inputs);
        var cacheRoot = Path.Combine(root, "artifacts", "HostTools", "RimWorldModdingMcp", "GatewayCompanion");
        var output = Path.Combine(cacheRoot, fingerprint);
        return new GatewayCompanionBuildPlan(
            project,
            cacheRoot,
            fingerprint,
            output,
            Path.Combine(output, "RimWorldDevGateway.Client.exe"),
            Path.Combine(output, "RimWorldDevGateway.Contracts.dll"),
            Path.Combine(output, ".complete"),
            Path.Combine(cacheRoot, fingerprint + ".lock"));
    }

    internal static IReadOnlyList<string> CreateCompanionBuildArguments(
        GatewayCompanionBuildPlan plan,
        string outputDirectory) =>
        ["build", plan.ProjectPath, "--configuration", "Release", "--framework", "net480",
            "--output", outputDirectory, "--nologo", "--verbosity", "minimal"];

    internal static async Task<GatewayCompanionBuildPlan> EnsureCompanionBuiltAsync(
        string repositoryRoot,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = RepositoryRoot.Resolve(repositoryRoot);
        var plan = CreateCompanionBuildPlan(root);
        Directory.CreateDirectory(plan.CacheRoot);
        using var lease = await AcquireBuildLeaseAsync(plan.LockPath, cancellationToken);
        if (IsPublishedCompanionValid(plan)) return plan;

        var staging = Path.Combine(plan.CacheRoot, plan.InputFingerprint + ".staging-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            var result = await ProcessRunner.RunAsync(
                "dotnet", CreateCompanionBuildArguments(plan, staging), root, TimeSpan.FromMinutes(5), cancellationToken);
            if (result.ExitCode != 0)
                throw new InvalidOperationException("Gateway companion client build failed: " +
                                                    Bound(result.StandardError + result.StandardOutput));
            var stagedClient = Path.Combine(staging, "RimWorldDevGateway.Client.exe");
            var stagedContract = Path.Combine(staging, "RimWorldDevGateway.Contracts.dll");
            if (!File.Exists(stagedClient) || !File.Exists(stagedContract))
                throw new InvalidOperationException("Gateway companion build did not produce the exact client and contract outputs.");
            var postBuildPlan = CreateCompanionBuildPlan(root);
            if (!string.Equals(postBuildPlan.InputFingerprint, plan.InputFingerprint, StringComparison.Ordinal))
                throw new InvalidOperationException("Gateway companion build inputs changed while the isolated build was running.");
            DurableFile.WriteAllText(
                Path.Combine(staging, ".complete"),
                BuildStamp(plan.InputFingerprint, Hash(stagedClient), Hash(stagedContract)));
            if (Directory.Exists(plan.OutputDirectory)) Directory.Delete(plan.OutputDirectory, recursive: true);
            Directory.Move(staging, plan.OutputDirectory);
            if (!IsPublishedCompanionValid(plan))
                throw new InvalidOperationException("Gateway companion atomic publication failed validation.");
            return plan;
        }
        finally
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
        }
    }

    internal static bool IsPublishedCompanionValid(GatewayCompanionBuildPlan plan)
    {
        if (!File.Exists(plan.ClientPath) || !File.Exists(plan.ContractPath) || !File.Exists(plan.StampPath)) return false;
        var lines = File.ReadAllLines(plan.StampPath);
        return lines.Length == 4 &&
               lines[0] == "RimWorldModdingMcp/GatewayCompanion/v1" &&
               lines[1] == plan.InputFingerprint &&
               lines[2] == Hash(plan.ClientPath) &&
               lines[3] == Hash(plan.ContractPath);
    }

    private Task<GatewayCompanionBuildPlan> EnsureCompanionAsync(CancellationToken cancellationToken) =>
        EnsureCompanionBuiltAsync(_repositoryRoot, cancellationToken);

    private static async Task<FileStream> AcquireBuildLeaseAsync(string path, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddMinutes(5);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(100, cancellationToken);
            }
            if (DateTimeOffset.UtcNow >= deadline)
                throw new TimeoutException("Timed out waiting for the exact Gateway companion build lease.");
        }
    }

    private static string Fingerprint(string root, IEnumerable<string> inputs)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var path in inputs)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(Path.GetRelativePath(root, path).Replace('\\', '/') + "\0"));
            hash.AppendData(File.ReadAllBytes(path));
            hash.AppendData([0]);
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static string BuildStamp(string fingerprint, string clientHash, string contractHash) =>
        string.Join('\n', "RimWorldModdingMcp/GatewayCompanion/v1", fingerprint, clientHash, contractHash);

    private static void RequireOk(JsonElement outer, string operation)
    {
        if (!outer.TryGetProperty("ok", out var ok) || ok.ValueKind != JsonValueKind.True)
            throw new InvalidOperationException($"{operation} failed: {Bound(outer.ToString())}");
    }

    private static string Bound(string value) => value.Length <= 8192 ? value.Trim() : value[..8192].Trim();
}

internal sealed record GatewayCompanionBuildPlan(
    string ProjectPath,
    string CacheRoot,
    string InputFingerprint,
    string OutputDirectory,
    string ClientPath,
    string ContractPath,
    string StampPath,
    string LockPath);
