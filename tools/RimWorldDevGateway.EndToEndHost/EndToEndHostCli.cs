using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;

namespace RimWorldDevGateway.EndToEndHost;

public static class EndToEndHostCli
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static RootCommand CreateCommand()
    {
        var root = new RootCommand(
            "Build, validate, stage, and clean dynamically loaded RimWorld E2E test bundles.");
        root.Subcommands.Add(CreatePlanCommand());
        root.Subcommands.Add(CreateStageCommand());
        root.Subcommands.Add(CreateCleanCommand());
        return root;
    }

    public static int Invoke(string[] args, TextWriter output, TextWriter error)
    {
        if (args is null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        var command = CreateCommand();
        var parseResult = command.Parse(args);
        if (parseResult.Errors.Count > 0)
        {
            foreach (var parseError in parseResult.Errors)
            {
                error.WriteLine(parseError.Message);
            }

            return 2;
        }

        return parseResult.Invoke(new InvocationConfiguration
        {
            Output = output ?? throw new ArgumentNullException(nameof(output)),
            Error = error ?? throw new ArgumentNullException(nameof(error))
        });
    }

    private static Command CreatePlanCommand()
    {
        var options = CommonOptions.Create();
        var command = new Command("plan", "Build tests and print deterministic exact-mod groups without changing a mod.");
        options.AddTo(command);
        command.SetAction(parseResult => Execute(
            parseResult,
            () =>
            {
                var plan = BuildPlan(options.Read(parseResult));
                WritePlan(parseResult, plan, options.ReadOutput(parseResult), "planned", leases: null);
                return 0;
            }));
        return command;
    }

    private static Command CreateStageCommand()
    {
        var options = CommonOptions.Create();
        var leaseFile = new Option<string>("--lease-file")
        {
            Description = "Required JSON lease path used later by the clean command.",
            Required = true
        };
        var command = new Command("stage", "Build tests and atomically publish marker-owned bundles into their owner mods.");
        options.AddTo(command);
        command.Options.Add(leaseFile);
        command.SetAction(parseResult => Execute(
            parseResult,
            () =>
            {
                var plan = BuildPlan(options.Read(parseResult));
                var leasePath = RequiredPath(parseResult.GetValue(leaseFile), "lease file");
                var publisher = new EndToEndStagePublisher();
                var leases = new List<EndToEndStageLease>();
                try
                {
                    foreach (var owner in plan.OwnerStages)
                    {
                        leases.Add(publisher.Publish(owner));
                    }

                    WriteLeaseFile(leasePath, leases);
                }
                catch
                {
                    foreach (var lease in leases.AsEnumerable().Reverse())
                    {
                        publisher.TryCleanup(lease);
                    }

                    throw;
                }

                WritePlan(parseResult, plan, options.ReadOutput(parseResult), "staged", leases);
                return 0;
            }));
        return command;
    }

    private static Command CreateCleanCommand()
    {
        var leaseFile = new Option<string>("--lease-file")
        {
            Description = "Lease JSON written by the stage command.",
            Required = true
        };
        var output = OutputOption();
        var command = new Command("clean", "Delete only stages whose marker still matches the exact lease.");
        command.Options.Add(leaseFile);
        command.Options.Add(output);
        command.SetAction(parseResult => Execute(
            parseResult,
            () =>
            {
                var leasePath = RequiredExistingFile(parseResult.GetValue(leaseFile), "lease file");
                var records = JsonSerializer.Deserialize<LeaseRecord[]>(File.ReadAllText(leasePath), JsonOptions) ??
                              throw new EndToEndStageException("The lease file contains no lease array.");
                if (records.Length == 0)
                {
                    throw new EndToEndStageException("The lease file contains zero leases.");
                }

                var publisher = new EndToEndStagePublisher();
                var failures = new List<string>();
                foreach (var record in records.AsEnumerable().Reverse())
                {
                    var lease = record.ToLease();
                    if (!publisher.TryCleanup(lease))
                    {
                        failures.Add(lease.DestinationDirectory);
                    }
                }

                if (failures.Count > 0)
                {
                    throw new EndToEndStageException(
                        "Refused to clean stages with missing or changed ownership markers: " +
                        string.Join(", ", failures));
                }

                File.Delete(leasePath);
                Write(parseResult, parseResult.GetValue(output), new
                {
                    status = "cleaned",
                    leaseFile = leasePath,
                    cleanedCount = records.Length
                });
                return 0;
            }));
        return command;
    }

    private static int Execute(ParseResult parseResult, Func<int> action)
    {
        try
        {
            return action();
        }
        catch (ArgumentException exception)
        {
            parseResult.InvocationConfiguration.Error.WriteLine(exception.Message);
            return 2;
        }
        catch (EndToEndDiscoveryException exception)
        {
            parseResult.InvocationConfiguration.Error.WriteLine(exception.Message);
            return 2;
        }
        catch (Exception exception)
        {
            parseResult.InvocationConfiguration.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static EndToEndBundlePlan BuildPlan(CommonInputs input)
    {
        var projects = EndToEndProjectDiscovery.Discover(input.RepositoryRoot);
        var assemblies = new EndToEndProjectBuilder().Build(
            projects,
            input.Configuration,
            TimeSpan.FromSeconds(input.BuildTimeoutSeconds));
        return EndToEndBundlePlanner.Create(
            assemblies,
            input.PackageIds,
            input.ModsRoot,
            input.RimWorldVersion);
    }

    private static void WritePlan(
        ParseResult parseResult,
        EndToEndBundlePlan plan,
        string output,
        string status,
        IReadOnlyList<EndToEndStageLease>? leases)
    {
        Write(parseResult, output, new
        {
            status,
            groups = plan.Discovery.Groups.Select(group => new
            {
                group.GroupId,
                activePackageIds = group.ActivePackageIds,
                tests = group.Tests.Select(test => test.Id).ToArray()
            }).ToArray(),
            owners = plan.OwnerStages.Select(owner => new
            {
                owner.OwnerPackageId,
                owner.DestinationDirectory,
                bundles = owner.Bundles.Select(bundle => bundle.AssemblyFileName).ToArray()
            }).ToArray(),
            leases = leases?.Select(LeaseRecord.From).ToArray() ?? Array.Empty<LeaseRecord>()
        });
    }

    private static void Write(ParseResult parseResult, string? output, object payload)
    {
        var normalized = NormalizeOutput(output);
        if (normalized == "json")
        {
            parseResult.InvocationConfiguration.Output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
            return;
        }

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(payload, JsonOptions));
        foreach (var property in document.RootElement.EnumerateObject())
        {
            parseResult.InvocationConfiguration.Output.WriteLine(
                property.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Object
                    ? $"{property.Name}: {property.Value.GetRawText()}"
                    : $"{property.Name}: {property.Value}");
        }
    }

    private static void WriteLeaseFile(string path, IReadOnlyList<EndToEndStageLease> leases)
    {
        if (leases.Count == 0)
        {
            throw new EndToEndStageException("Stage publication produced zero leases.");
        }

        var parent = Path.GetDirectoryName(path) ??
                     throw new EndToEndStageException("The lease file has no parent directory.");
        Directory.CreateDirectory(parent);
        var temporary = path + ".tmp." + Guid.NewGuid().ToString("N");
        File.WriteAllText(
            temporary,
            JsonSerializer.Serialize(leases.Select(LeaseRecord.From).ToArray(), JsonOptions));
        File.Move(temporary, path, overwrite: true);
    }

    private static string RequiredPath(string? value, string description) =>
        Path.GetFullPath(string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"A {description} is required.")
            : value);

    private static string RequiredExistingDirectory(string? value, string description)
    {
        var path = RequiredPath(value, description);
        return Directory.Exists(path)
            ? path
            : throw new ArgumentException($"The {description} does not exist: {path}");
    }

    private static string RequiredExistingFile(string? value, string description)
    {
        var path = RequiredPath(value, description);
        return File.Exists(path)
            ? path
            : throw new ArgumentException($"The {description} does not exist: {path}");
    }

    private static string NormalizeOutput(string? value)
    {
        var output = string.IsNullOrWhiteSpace(value) ? "table" : value.Trim().ToLowerInvariant();
        return output is "json" or "table"
            ? output
            : throw new ArgumentException("Output must be 'json' or 'table'.");
    }

    private static Option<string> OutputOption() => new("--output", "-o")
    {
        Description = "Output format: table (default) or json.",
        DefaultValueFactory = _ => "table"
    };

    private sealed class CommonOptions
    {
        private CommonOptions()
        {
        }

        public Option<string> RepositoryRoot { get; } = RequiredOption(
            "--repository-root",
            "Repository root scanned for marked E2E projects.");

        public Option<string> ModsRoot { get; } = RequiredOption(
            "--mods-root",
            "RimWorld Mods directory containing the owner mod folders.");

        public Option<string> RimWorldVersion { get; } = RequiredOption(
            "--rimworld-version",
            "Versioned mod folder, for example 1.6.");

        public Option<string[]> PackageIds { get; } = new("--package-id")
        {
            Description = "Resolvable package ID. Repeat for every package used by selected groups.",
            Required = true,
            AllowMultipleArgumentsPerToken = true
        };

        public Option<string> Configuration { get; } = new("--configuration")
        {
            Description = "MSBuild configuration.",
            DefaultValueFactory = _ => "Release"
        };

        public Option<int> BuildTimeoutSeconds { get; } = new("--build-timeout-seconds")
        {
            Description = "Per build/property-query timeout.",
            DefaultValueFactory = _ => 120
        };

        public Option<string> Output { get; } = OutputOption();

        public static CommonOptions Create() => new();

        public void AddTo(Command command)
        {
            command.Options.Add(RepositoryRoot);
            command.Options.Add(ModsRoot);
            command.Options.Add(RimWorldVersion);
            command.Options.Add(PackageIds);
            command.Options.Add(Configuration);
            command.Options.Add(BuildTimeoutSeconds);
            command.Options.Add(Output);
        }

        public CommonInputs Read(ParseResult result)
        {
            var timeout = result.GetValue(BuildTimeoutSeconds);
            if (timeout is < 10 or > 900)
            {
                throw new ArgumentException("Build timeout must be between 10 and 900 seconds.");
            }

            var packageIds = (result.GetValue(PackageIds) ?? Array.Empty<string>())
                .Select(value => value?.Trim().ToLowerInvariant() ?? string.Empty)
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (packageIds.Length == 0)
            {
                throw new ArgumentException("At least one --package-id is required.");
            }

            return new CommonInputs(
                RequiredExistingDirectory(result.GetValue(RepositoryRoot), "repository root"),
                RequiredExistingDirectory(result.GetValue(ModsRoot), "Mods root"),
                RequiredToken(result.GetValue(RimWorldVersion), "RimWorld version"),
                packageIds,
                RequiredToken(result.GetValue(Configuration), "configuration"),
                timeout);
        }

        public string ReadOutput(ParseResult result) => NormalizeOutput(result.GetValue(Output));

        private static Option<string> RequiredOption(string name, string description) => new(name)
        {
            Description = description,
            Required = true
        };

        public static string RequiredToken(string? value, string description) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException($"A {description} is required.")
                : value.Trim();
    }

    private sealed record CommonInputs(
        string RepositoryRoot,
        string ModsRoot,
        string RimWorldVersion,
        IReadOnlyList<string> PackageIds,
        string Configuration,
        int BuildTimeoutSeconds);

    private sealed record LeaseRecord(
        string DestinationDirectory,
        string OwnerPackageId,
        string RimWorldVersion,
        string TransactionId)
    {
        public static LeaseRecord From(EndToEndStageLease lease) => new(
            lease.DestinationDirectory,
            lease.OwnerPackageId,
            lease.RimWorldVersion,
            lease.TransactionId);

        public EndToEndStageLease ToLease() => new(
            RequiredPath(DestinationDirectory, "lease destination"),
            CommonOptions.RequiredToken(OwnerPackageId, "lease owner package ID"),
            CommonOptions.RequiredToken(RimWorldVersion, "lease RimWorld version"),
            CommonOptions.RequiredToken(TransactionId, "lease transaction ID"));
    }
}
