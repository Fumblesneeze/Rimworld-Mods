using System.Text.Json;

namespace RimWorldModding.Mcp;

public sealed record SubscriberAutomationStep(
    string Operation,
    string ExpectedStatus,
    string? Screenshot);

public sealed record SubscriberVerificationManifest(
    string Schema,
    string Engine,
    string? Source,
    string? EntryType,
    string? Automation,
    string? Script,
    IReadOnlyList<string> AdditionalPackageIds,
    IReadOnlyList<SubscriberAutomationStep> Steps,
    string ExpectedObservation,
    int TimeoutSeconds);

public sealed record SubscriberVerificationPlan(
    string Engine,
    string WorkshopPackagePath,
    string? Source,
    string? EntryType,
    string? Automation,
    string? Script,
    IReadOnlyList<string> PackageIds,
    IReadOnlyList<SubscriberAutomationStep> Steps,
    string ExpectedObservation,
    int TimeoutSeconds)
{
    public static SubscriberVerificationPlan Create(
        string repositoryRoot,
        ReleaseProfile profile,
        SubscriberVerificationManifest manifest,
        string publishedFileId,
        string? workshopPackagePath = null)
    {
        _ = RepositoryRoot.Resolve(repositoryRoot);
        if (publishedFileId.Length < 6 || publishedFileId.Any(character => !char.IsAsciiDigit(character)))
            throw new InvalidOperationException("Published Workshop item ID is invalid.");
        var resolvedWorkshopPath = Path.GetFullPath(workshopPackagePath ??
            Path.Combine(@"F:\Steam\steamapps\workshop\content", profile.SteamAppId.ToString(), publishedFileId));
        var packages = manifest.AdditionalPackageIds
            .Append(profile.PackageId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new SubscriberVerificationPlan(
            manifest.Engine,
            resolvedWorkshopPath,
            manifest.Source,
            manifest.EntryType,
            manifest.Automation,
            manifest.Script,
            packages,
            manifest.Steps,
            manifest.ExpectedObservation,
            manifest.TimeoutSeconds);
    }
}

public static class SubscriberVerificationProfiles
{
    public static SubscriberVerificationManifest Freeze(
        string repositoryRoot,
        SubscriberVerificationManifest manifest,
        string destinationProfilePath)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        var destination = RepositoryRoot.ContainedPath(root, destinationProfilePath);
        var directory = Path.GetDirectoryName(destination)!;
        Directory.CreateDirectory(directory);
        string? FreezeInput(string? input, string stem)
        {
            if (input is null) return null;
            var output = Path.Combine(directory, stem + Path.GetExtension(input));
            File.Copy(input, output, overwrite: false);
            return Path.GetRelativePath(root, output).Replace('\\', '/');
        }
        var source = FreezeInput(manifest.Source, "automation-source");
        var script = FreezeInput(manifest.Script, "subscriber-script");
        var payload = new Dictionary<string, object?>
        {
            ["schema"] = manifest.Schema,
            ["engine"] = manifest.Engine,
            ["source"] = source,
            ["entryType"] = manifest.EntryType,
            ["automation"] = manifest.Automation,
            ["script"] = script,
            ["additionalPackageIds"] = manifest.AdditionalPackageIds,
            ["steps"] = manifest.Steps.Select(step => new Dictionary<string, object?>
            {
                ["operation"] = step.Operation,
                ["expectedStatus"] = step.ExpectedStatus,
                ["screenshot"] = step.Screenshot
            }).ToArray(),
            ["expectedObservation"] = manifest.ExpectedObservation,
            ["timeoutSeconds"] = manifest.TimeoutSeconds
        };
        DurableFile.WriteAllText(destination,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
        return Load(root, destination);
    }

    public static SubscriberVerificationManifest Load(string repositoryRoot, string path)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        var resolved = RepositoryRoot.ContainedPath(root, path);
        if (!File.Exists(resolved)) throw new InvalidOperationException("Subscriber verification profile does not exist.");
        using var document = JsonDocument.Parse(File.ReadAllText(resolved));
        var element = document.RootElement;
        var allowed = new HashSet<string>(
            ["schema", "engine", "source", "entryType", "automation", "script", "additionalPackageIds", "steps", "expectedObservation", "timeoutSeconds"],
            StringComparer.Ordinal);
        var unknown = element.EnumerateObject().Select(property => property.Name).Where(name => !allowed.Contains(name)).ToArray();
        if (unknown.Length > 0) throw new InvalidOperationException("Subscriber verification profile has unknown fields: " + string.Join(", ", unknown));
        string Required(string name) => element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String &&
                                        !string.IsNullOrWhiteSpace(property.GetString())
            ? property.GetString()!.Trim()
            : throw new InvalidOperationException($"Subscriber verification profile requires '{name}'.");
        string? Optional(string name) => element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()?.Trim()
            : null;
        var schema = Required("schema");
        if (schema != "RimWorldSubscriberVerification/v1") throw new InvalidOperationException("Subscriber verification schema is unsupported.");
        var engine = Required("engine");
        if (engine is not ("gateway-automation" or "powershell-subscriber")) throw new InvalidOperationException("Subscriber verification engine is unsupported.");
        var packages = element.GetProperty("additionalPackageIds").EnumerateArray().Select(value => value.GetString()!.Trim()).ToArray();
        if (packages.Any(value => string.IsNullOrWhiteSpace(value))) throw new InvalidOperationException("Subscriber package IDs are invalid.");
        var steps = element.GetProperty("steps").EnumerateArray().Select(step => new SubscriberAutomationStep(
            step.GetProperty("operation").GetString()!.Trim(),
            step.GetProperty("expectedStatus").GetString()!.Trim(),
            step.TryGetProperty("screenshot", out var screenshot) ? screenshot.GetString()?.Trim() : null)).ToArray();
        if (steps.Any(step => string.IsNullOrWhiteSpace(step.Operation) || string.IsNullOrWhiteSpace(step.ExpectedStatus)))
            throw new InvalidOperationException("Subscriber automation steps require operation and expectedStatus.");
        var screenshotNames = steps.Where(step => step.Screenshot is not null).Select(step => step.Screenshot!).ToArray();
        if (screenshotNames.Any(name => Path.IsPathRooted(name) || name.Split('/', '\\').Any(part => part == "..")) ||
            screenshotNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != screenshotNames.Length)
            throw new InvalidOperationException("Subscriber screenshot paths must be unique contained relative paths.");
        if (engine == "gateway-automation" && (steps.Length == 0 || Optional("source") is null || Optional("entryType") is null || Optional("automation") is null))
            throw new InvalidOperationException("Gateway subscriber verification requires source, entryType, automation, and steps.");
        if (engine == "gateway-automation" && steps.Count(step => step.Operation == "cleanup") != 1)
            throw new InvalidOperationException("Gateway subscriber verification requires exactly one cleanup step.");
        if (engine == "powershell-subscriber" && Optional("script") is null)
            throw new InvalidOperationException("PowerShell subscriber verification requires a script adapter.");
        var timeout = element.TryGetProperty("timeoutSeconds", out var timeoutProperty) && timeoutProperty.TryGetInt32(out var parsed)
            ? parsed
            : 600;
        if (timeout is < 60 or > 1800) throw new InvalidOperationException("Subscriber verification timeout must be 60-1800 seconds.");
        var resolvedSource = Optional("source") is { } source ? RepositoryRoot.ContainedPath(root, source) : null;
        var resolvedScript = Optional("script") is { } script ? RepositoryRoot.ContainedPath(root, script) : null;
        if (resolvedSource is not null && !File.Exists(resolvedSource))
            throw new InvalidOperationException("Subscriber verification source does not exist.");
        if (resolvedScript is not null && !File.Exists(resolvedScript))
            throw new InvalidOperationException("Subscriber verification script does not exist.");
        return new SubscriberVerificationManifest(
            schema,
            engine,
            resolvedSource,
            Optional("entryType"),
            Optional("automation"),
            resolvedScript,
            packages,
            steps,
            Required("expectedObservation"),
            timeout);
    }

    public static IReadOnlyList<CandidateFile> CaptureInputs(
        string repositoryRoot,
        string profilePath,
        SubscriberVerificationManifest manifest)
    {
        var root = RepositoryRoot.Resolve(repositoryRoot);
        var inputs = new[] { RepositoryRoot.ContainedPath(root, profilePath), manifest.Source, manifest.Script }
            .Where(path => path is not null)
            .Select(path => Path.GetFullPath(path!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return inputs.Select(path => new CandidateFile(
            Path.GetRelativePath(root, path).Replace('\\', '/'),
            new FileInfo(path).Length,
            ReleaseCandidateBuilder.Hash(path))).ToArray();
    }
}
