using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;

namespace RimWorldModding.Mcp;

public static class McpCli
{
    public static Task<int> InvokeAsync(
        string[] args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        var root = new RootCommand("One typed MCP/CLI for RimWorld mod development, verification, and release operations.");
        root.Subcommands.Add(CreateServeCommand());
        root.Subcommands.Add(CreateToolCommand());
        root.Subcommands.Add(CreateReleaseWorkerCommand());
        var parseResult = root.Parse(args);
        if (parseResult.Errors.Count > 0)
        {
            foreach (var parseError in parseResult.Errors)
            {
                error.WriteLine(parseError.Message);
            }

            return Task.FromResult(2);
        }

        var exitCode = parseResult.Invoke(new InvocationConfiguration { Output = output, Error = error });
        return Task.FromResult(exitCode);
    }

    private static Command CreateServeCommand()
    {
        var repositoryRoot = RepositoryRootOption();
        var command = new Command("serve", "Start the local stdio MCP server for this exact repository.");
        command.Options.Add(repositoryRoot);
        command.SetAction(parseResult => Execute(parseResult, async cancellationToken =>
        {
            var root = RepositoryRoot.Resolve(parseResult.GetValue(repositoryRoot));
            await McpServerHost.RunAsync(root, cancellationToken);
            return 0;
        }));
        return command;
    }

    private static Command CreateToolCommand()
    {
        var tool = new Command("tool", "List or invoke the same typed operations exposed over MCP.");
        tool.Subcommands.Add(CreateListCommand());
        tool.Subcommands.Add(CreateCallCommand());
        return tool;
    }

    private static Command CreateReleaseWorkerCommand()
    {
        var request = new Argument<string>("request") { Description = "Canonical durable worker request path." };
        var command = new Command("release-worker", "Internal durable release worker.");
        command.Arguments.Add(request);
        command.SetAction(parseResult => Execute(parseResult,
            _ => ReleaseWorkerCoordinator.RunAsync(parseResult.GetValue(request)!)));
        return command;
    }

    private static Command CreateListCommand()
    {
        var repositoryRoot = RepositoryRootOption();
        var output = OutputOption();
        var command = new Command("list", "List operation names, safety classes, timeouts, and evidence policy.");
        command.Options.Add(repositoryRoot);
        command.Options.Add(output);
        command.SetAction(parseResult => Execute(parseResult, cancellationToken =>
        {
            var registry = OperationRegistry.CreateDefault(parseResult.GetValue(repositoryRoot)!);
            WriteResult(parseResult, registry.Descriptors.ToList(), NormalizeOutput(parseResult.GetValue(output)));
            return Task.FromResult(0);
        }));
        return command;
    }

    private static Command CreateCallCommand()
    {
        var name = new Argument<string>("name") { Description = "Exact operation name from tool list." };
        var repositoryRoot = RepositoryRootOption();
        var arguments = new Option<string>("--arguments")
        {
            Description = "One JSON object containing the operation arguments.",
            DefaultValueFactory = _ => "{}"
        };
        var output = OutputOption();
        var command = new Command("call", "Invoke one typed operation by exact name.");
        command.Arguments.Add(name);
        command.Options.Add(repositoryRoot);
        command.Options.Add(arguments);
        command.Options.Add(output);
        command.SetAction(parseResult => Execute(parseResult, async cancellationToken =>
        {
            var normalizedOutput = NormalizeOutput(parseResult.GetValue(output));
            var json = parseResult.GetValue(arguments) ?? "{}";
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(json);
            }
            catch (JsonException exception)
            {
                throw new ArgumentException($"Arguments must be valid JSON: {exception.Message}");
            }

            using (document)
            {
                var registry = OperationRegistry.CreateDefault(parseResult.GetValue(repositoryRoot)!);
                var result = await registry.InvokeAsync(parseResult.GetValue(name)!, document.RootElement, cancellationToken);
                WriteResult(parseResult, result, normalizedOutput);
            }

            return 0;
        }));
        return command;
    }

    private static int Execute(ParseResult parseResult, Func<CancellationToken, Task<int>> action)
    {
        try
        {
            return action(CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (ArgumentException exception)
        {
            parseResult.InvocationConfiguration.Error.WriteLine(exception.Message);
            return 2;
        }
        catch (Exception exception)
        {
            parseResult.InvocationConfiguration.Error.WriteLine(exception.GetBaseException().Message);
            return 1;
        }
    }

    private static void WriteResult(ParseResult parseResult, object value, string output)
    {
        if (output == "json")
        {
            parseResult.InvocationConfiguration.Output.WriteLine(OperationJson.Serialize(value));
            return;
        }

        switch (value)
        {
            case RepositoryStatusResult status:
                parseResult.InvocationConfiguration.Output.WriteLine($"repository: {status.RepositoryRoot}");
                parseResult.InvocationConfiguration.Output.WriteLine($"revision: {status.HeadRevision}");
                parseResult.InvocationConfiguration.Output.WriteLine($"clean: {status.WorktreeClean}");
                parseResult.InvocationConfiguration.Output.WriteLine($"mods: {status.Mods.Count}");
                break;
            case List<OperationDescriptor> descriptors:
                parseResult.InvocationConfiguration.Output.WriteLine("name                 risk            long  timeout");
                foreach (var descriptor in descriptors)
                {
                    parseResult.InvocationConfiguration.Output.WriteLine(
                        $"{descriptor.Name,-20} {descriptor.Risk,-15} {descriptor.LongRunning,-5} {descriptor.TimeoutSeconds,7}");
                }
                break;
            case ReleaseProfileResult profile:
                parseResult.InvocationConfiguration.Output.WriteLine($"package: {profile.PackageId}");
                parseResult.InvocationConfiguration.Output.WriteLine($"title: {profile.Title}");
                parseResult.InvocationConfiguration.Output.WriteLine($"visibility: {profile.Visibility}");
                parseResult.InvocationConfiguration.Output.WriteLine($"first publication: {profile.AllowFirstPublication}");
                parseResult.InvocationConfiguration.Output.WriteLine($"profile: {profile.ProfilePath}");
                break;
            case ModListState state:
                parseResult.InvocationConfiguration.Output.WriteLine($"config: {state.ConfigPath}");
                parseResult.InvocationConfiguration.Output.WriteLine($"sha256: {state.Sha256}");
                parseResult.InvocationConfiguration.Output.WriteLine($"active mods: {state.ActiveMods.Count}");
                break;
            case ModListMutationResult mutation:
                parseResult.InvocationConfiguration.Output.WriteLine($"config: {mutation.ConfigPath}");
                parseResult.InvocationConfiguration.Output.WriteLine($"changed: {mutation.Changed}");
                parseResult.InvocationConfiguration.Output.WriteLine($"backup: {mutation.BackupPath ?? "(none)"}");
                parseResult.InvocationConfiguration.Output.WriteLine($"before: {mutation.BeforeSha256}");
                parseResult.InvocationConfiguration.Output.WriteLine($"after: {mutation.AfterSha256}");
                break;
            case LocalModInstallResult install:
                parseResult.InvocationConfiguration.Output.WriteLine($"package: {install.PackageId}");
                parseResult.InvocationConfiguration.Output.WriteLine($"destination: {install.Destination}");
                parseResult.InvocationConfiguration.Output.WriteLine($"backup: {install.BackupPath ?? "(none)"}");
                parseResult.InvocationConfiguration.Output.WriteLine($"files: {install.Files.Count}");
                break;
            case ModBuildResult build:
                parseResult.InvocationConfiguration.Output.WriteLine($"build: {build.Build.Operation} ({build.Build.ExitCode})");
                parseResult.InvocationConfiguration.Output.WriteLine($"package: {build.Installation.PackageId}");
                parseResult.InvocationConfiguration.Output.WriteLine($"destination: {build.Installation.Destination}");
                parseResult.InvocationConfiguration.Output.WriteLine($"files: {build.Installation.Files.Count}");
                break;
            case ReleasePreparationResult preparation:
                parseResult.InvocationConfiguration.Output.WriteLine($"status: {preparation.Status}");
                parseResult.InvocationConfiguration.Output.WriteLine($"title: {preparation.Title}");
                parseResult.InvocationConfiguration.Output.WriteLine($"plan: {preparation.PlanPath}");
                parseResult.InvocationConfiguration.Output.WriteLine($"plan sha256: {preparation.PlanSha256}");
                parseResult.InvocationConfiguration.Output.WriteLine($"candidate: {preparation.CandidateDigest}");
                parseResult.InvocationConfiguration.Output.WriteLine($"nonce: {preparation.ConfirmationNonce}");
                parseResult.InvocationConfiguration.Output.WriteLine($"expires: {preparation.ExpiresUtc:O}");
                break;
            case AdapterOperationResult adapter:
                parseResult.InvocationConfiguration.Output.WriteLine($"operation: {adapter.Operation}");
                parseResult.InvocationConfiguration.Output.WriteLine($"exit code: {adapter.ExitCode}");
                parseResult.InvocationConfiguration.Output.WriteLine($"duration seconds: {adapter.DurationSeconds:F3}");
                parseResult.InvocationConfiguration.Output.WriteLine($"evidence: {adapter.EvidenceRoot ?? "(none)"}");
                break;
            case RunStartResult runStart:
                parseResult.InvocationConfiguration.Output.WriteLine($"run: {runStart.RunId}");
                parseResult.InvocationConfiguration.Output.WriteLine($"state: {runStart.State}");
                parseResult.InvocationConfiguration.Output.WriteLine($"launcher pid: {runStart.LauncherProcessId}");
                parseResult.InvocationConfiguration.Output.WriteLine($"artifacts: {runStart.RunRoot}");
                break;
            case RunStatusResult runStatus:
                parseResult.InvocationConfiguration.Output.WriteLine($"run: {runStatus.RunId}");
                parseResult.InvocationConfiguration.Output.WriteLine($"state: {runStatus.State}");
                parseResult.InvocationConfiguration.Output.WriteLine($"game pid: {runStatus.GameProcessId?.ToString() ?? "(not ready)"}");
                parseResult.InvocationConfiguration.Output.WriteLine($"manifest: {runStatus.GatewayManifestPath ?? "(not ready)"}");
                break;
            case ReleaseCandidateStage candidate:
                parseResult.InvocationConfiguration.Output.WriteLine($"package: {candidate.PackagePath}");
                parseResult.InvocationConfiguration.Output.WriteLine($"digest: {candidate.ContentDigest}");
                parseResult.InvocationConfiguration.Output.WriteLine($"files: {candidate.Files.Count}");
                break;
            case EvidenceReadResult evidence:
                parseResult.InvocationConfiguration.Output.WriteLine($"path: {evidence.Path}");
                parseResult.InvocationConfiguration.Output.WriteLine($"bytes: {evidence.Bytes}");
                parseResult.InvocationConfiguration.Output.WriteLine($"sha256: {evidence.Sha256}");
                parseResult.InvocationConfiguration.Output.WriteLine(evidence.Content);
                break;
            case ReleasePlanStatusResult releaseStatus:
                parseResult.InvocationConfiguration.Output.WriteLine($"status: {releaseStatus.Status}");
                parseResult.InvocationConfiguration.Output.WriteLine($"title: {releaseStatus.Title}");
                parseResult.InvocationConfiguration.Output.WriteLine($"plan sha256: {releaseStatus.PlanSha256}");
                parseResult.InvocationConfiguration.Output.WriteLine($"candidate: {releaseStatus.CandidateDigest}");
                parseResult.InvocationConfiguration.Output.WriteLine($"durable state: {releaseStatus.DurableState}");
                parseResult.InvocationConfiguration.Output.WriteLine($"release worker: {releaseStatus.ReleaseWorkerState}");
                break;
            case ReleasePublishResult releasePublish:
                parseResult.InvocationConfiguration.Output.WriteLine($"status: {releasePublish.Status}");
                parseResult.InvocationConfiguration.Output.WriteLine($"item: {releasePublish.PublishedFileId}");
                parseResult.InvocationConfiguration.Output.WriteLine($"url: {releasePublish.WorkshopUrl}");
                parseResult.InvocationConfiguration.Output.WriteLine($"receipt: {releasePublish.ReceiptPath}");
                parseResult.InvocationConfiguration.Output.WriteLine($"steam modified before: {releasePublish.SteamModifiedBeforeUnixSeconds?.ToString() ?? "first publication"}");
                parseResult.InvocationConfiguration.Output.WriteLine($"steam modified after: {releasePublish.SteamModifiedAfterUnixSeconds}");
                break;
            case WorkshopPresentationSyncResult presentationSync:
                foreach (var line in DescribePresentationSync(presentationSync))
                    parseResult.InvocationConfiguration.Output.WriteLine(line);
                break;
            case GatewayRawMutationResult rawMutation:
                parseResult.InvocationConfiguration.Output.WriteLine($"run: {rawMutation.RunId}");
                parseResult.InvocationConfiguration.Output.WriteLine($"source sha256: {rawMutation.SourceSha256}");
                parseResult.InvocationConfiguration.Output.WriteLine($"entry: {rawMutation.EntryType}");
                parseResult.InvocationConfiguration.Output.WriteLine("gameplay acceptance evidence: false");
                break;
            default:
                throw new InvalidOperationException($"No table projection is registered for {value.GetType().FullName}.");
        }
    }

    internal static string[] DescribePresentationSync(WorkshopPresentationSyncResult result) =>
    [
        $"status: {result.Status}",
        $"item: {result.PublishedFileId}",
        $"url: {result.WorkshopUrl}",
        $"previews: {result.PreviewCount}",
        $"description: {result.DescriptionPath}",
        $"inventory: {result.InventoryPath}"
    ];

    private static string NormalizeOutput(string? value)
    {
        var output = string.IsNullOrWhiteSpace(value) ? "table" : value.Trim().ToLowerInvariant();
        return output is "json" or "table"
            ? output
            : throw new ArgumentException("Output must be 'json' or 'table'.");
    }

    private static Option<string> RepositoryRootOption() => new("--repository-root")
    {
        Description = "Exact repository root. Defaults to the current directory.",
        DefaultValueFactory = _ => Environment.CurrentDirectory
    };

    private static Option<string> OutputOption() => new("--output", "-o")
    {
        Description = "Output format: table (default) or json.",
        DefaultValueFactory = _ => "table"
    };
}
