using System.Text.Json;
using System.Text.RegularExpressions;

namespace RimWorldModding.Mcp;

public static class GatewayClientPlanner
{
    private static readonly Regex LiteralName = new("^[A-Za-z0-9][A-Za-z0-9._-]{0,199}$", RegexOptions.CultureInvariant);

    public static AdapterCommand Diagnostic(
        string repositoryRoot,
        string manifestPath,
        int processId,
        string command,
        string? argumentsJson)
    {
        if (command is not ("discover" or "status" or "ui-state" or "logs" or "automations"))
            throw new ArgumentException("Diagnostic command must be discover, status, ui-state, logs, or automations.");
        if (argumentsJson is not null)
            throw new ArgumentException("This diagnostic command does not accept free-form arguments.");
        return Plan(repositoryRoot, manifestPath, processId, "gateway_diagnostic", [command]);
    }

    public static AdapterCommand Mutation(
        string repositoryRoot,
        string manifestPath,
        int processId,
        string kind,
        string name,
        string argumentsJson)
    {
        if (kind is not ("action" or "automation"))
            throw new ArgumentException("Mutation kind must be action or automation.");
        if (!LiteralName.IsMatch(name)) throw new ArgumentException("Mutation name must be one bounded literal name.");
        ValidateArguments(argumentsJson);
        var command = kind == "action" ? "action" : "run";
        return Plan(repositoryRoot, manifestPath, processId, "gateway_mutation", [command, name, "--arguments", argumentsJson]);
    }

    public static AdapterCommand DebugActions(string repositoryRoot, string manifestPath, int processId,
        string? search, string[] categories, string[] modes, string[] allowedGameStates, string? after, int limit)
    {
        var arguments = new List<string> { "debug-actions", "query", "--limit", limit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--categories-json", JsonSerializer.Serialize(categories), "--modes-json", JsonSerializer.Serialize(modes),
            "--allowed-game-states-json", JsonSerializer.Serialize(allowedGameStates) };
        if (search is not null) { arguments.Add("--search"); arguments.Add(search); }
        if (after is not null) { arguments.Add("--after"); arguments.Add(after); }
        return Plan(repositoryRoot, manifestPath, processId, "gateway_debug_actions", arguments);
    }

    public static AdapterCommand DebugActionInvoke(string repositoryRoot, string manifestPath, int processId, string handle)
    {
        if (string.IsNullOrWhiteSpace(handle) || handle is "." or ".." || handle.Any(character => char.IsControl(character) || character is '/' or '\\'))
            throw new ArgumentException("handle must be one exact nonblank native path segment, without slashes, dot segments or control characters.");
        return Plan(repositoryRoot, manifestPath, processId, "gateway_debug_action_invoke", ["debug-actions", "invoke", handle]);
    }

    private static AdapterCommand Plan(
        string repositoryRoot,
        string manifestPath,
        int processId,
        string kind,
        IReadOnlyList<string> commandArguments)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        var manifest = RepositoryRoot.ContainedPath(root, manifestPath);
        if (!File.Exists(manifest) || !manifest.EndsWith("current.json", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("manifestPath must select one existing Gateway current.json under the repository.");
        if (processId <= 0) throw new ArgumentException("processId must be positive.");
        var project = Path.Combine(root, "tools", "RimWorldDevGateway.Client", "RimWorldDevGateway.Client.csproj");
        if (!File.Exists(project)) throw new InvalidOperationException("Gateway companion client project is missing.");
        var arguments = new List<string>
        {
            "run", "--project", project, "-c", "Release", "--"
        };
        arguments.AddRange(commandArguments);
        arguments.Add("--manifest");
        arguments.Add(manifest);
        arguments.Add("--pid");
        arguments.Add(processId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        arguments.Add("--output");
        arguments.Add("json");
        return new AdapterCommand(kind, "dotnet", arguments, root, TimeSpan.FromMinutes(3));
    }

    private static void ValidateArguments(string json)
    {
        if (json.Length > 65536) throw new ArgumentException("Mutation arguments exceed 64 KiB.");
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Mutation arguments must be one JSON object.");
    }
}
