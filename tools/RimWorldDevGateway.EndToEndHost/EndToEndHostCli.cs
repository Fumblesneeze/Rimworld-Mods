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
        root.Subcommands.Add(CreatePerformancePlanCommand());
        root.Subcommands.Add(CreatePerformanceStageCommand());
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

    private static Command CreatePerformancePlanCommand()
    {
        var common = CommonOptions.Create();
        var artifactRoot = new Option<string>("--artifact-root")
        {
            Description = "Root for deterministic raw, normalized, CSV, Markdown, and aggregate report paths.",
            Required = true
        };
        var benchmarkIds = new Option<string[]>("--benchmark-id")
        {
            Description = "Exact benchmark ID filter. Repeat to select more than one benchmark.",
            AllowMultipleArgumentsPerToken = true
        };
        var groupIds = new Option<string[]>("--group-id")
        {
            Description = "Exact performance group ID filter. Repeat to select more than one group.",
            AllowMultipleArgumentsPerToken = true
        };
        var warmUpTicks = new Option<int?>("--warm-up-ticks")
        {
            Description = "Optional non-negative warm-up tick override."
        };
        var sampleTicks = new Option<int?>("--sample-ticks")
        {
            Description = "Optional positive sample tick override."
        };
        var repetitions = new Option<int?>("--repetitions")
        {
            Description = $"Optional repetition override from 1 to {PerformanceRunPlanBuilder.MaximumRepetitions}."
        };
        var command = new Command(
            "performance-plan",
            "Build marked performance fixtures and print fresh-process benchmark/repetition plans without staging or launching.");
        common.AddTo(command);
        command.Options.Add(artifactRoot);
        command.Options.Add(benchmarkIds);
        command.Options.Add(groupIds);
        command.Options.Add(warmUpTicks);
        command.Options.Add(sampleTicks);
        command.Options.Add(repetitions);
        command.SetAction(parseResult => Execute(parseResult, () =>
        {
            var input = common.Read(parseResult);
            var projects = PerformanceProjectDiscovery.Discover(input.RepositoryRoot);
            var candidates = new EndToEndProjectBuilder().BuildPerformance(
                projects,
                input.Configuration,
                TimeSpan.FromSeconds(input.BuildTimeoutSeconds));
            var discovery = PerformanceDiscoveryValidator.ValidateAndGroup(candidates, input.PackageIds);
            var plan = PerformanceRunPlanBuilder.Create(
                discovery,
                parseResult.GetValue(benchmarkIds) ?? Array.Empty<string>(),
                parseResult.GetValue(groupIds) ?? Array.Empty<string>(),
                parseResult.GetValue(warmUpTicks),
                parseResult.GetValue(sampleTicks),
                parseResult.GetValue(repetitions),
                RequiredPath(parseResult.GetValue(artifactRoot), "artifact root"));
            WritePerformancePlan(parseResult, plan, common.ReadOutput(parseResult));
            return 0;
        }));
        return command;
    }

    private static Command CreatePerformanceStageCommand()
    {
        var common = CommonOptions.Create();
        var leaseFile = new Option<string>("--lease-file")
        {
            Description = "Required JSON lease path used later by the clean command.",
            Required = true
        };
        var command = new Command(
            "performance-stage",
            "Build marked performance fixtures and atomically publish their exact manifests for one run.");
        common.AddTo(command);
        command.Options.Add(leaseFile);
        command.SetAction(parseResult => Execute(parseResult, () =>
        {
            var input = common.Read(parseResult);
            var projects = PerformanceProjectDiscovery.Discover(input.RepositoryRoot);
            var candidates = new EndToEndProjectBuilder().BuildPerformance(
                projects,
                input.Configuration,
                TimeSpan.FromSeconds(input.BuildTimeoutSeconds));
            var plan = PerformanceBundlePlanner.Create(
                candidates,
                input.PackageIds,
                input.ModsRoot,
                input.RimWorldVersion);
            var publisher = new EndToEndStagePublisher();
            var leasePath = RequiredPath(parseResult.GetValue(leaseFile), "lease file");
            var leases = PerformanceStageTransaction.PublishAll(
                plan.OwnerStages,
                publisher.Publish,
                publisher.TryCleanup,
                staged => WriteLeaseFile(leasePath, staged),
                () => ClearLeaseFile(leasePath));

            Write(parseResult, common.ReadOutput(parseResult), new
            {
                status = "staged",
                groups = plan.Discovery.Groups.Select(group => new
                {
                    group.GroupId,
                    activePackageIds = group.ActivePackageIds,
                    benchmarks = group.Benchmarks.Select(item => item.Id).ToArray()
                }).ToArray(),
                owners = plan.OwnerStages.Select(owner => new
                {
                    owner.OwnerPackageId,
                    owner.DestinationDirectory,
                    bundles = owner.Bundles.Select(bundle => bundle.AssemblyFileName).ToArray()
                }).ToArray(),
                leases = leases.Select(LeaseRecord.From).ToArray()
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

    private static void WritePerformancePlan(ParseResult parseResult, PerformanceRunPlan plan, string output)
    {
        Write(parseResult, output, new
        {
            status = "planned",
            mutatedGame = false,
            groups = plan.Groups.Select(group => new
            {
                group.GroupId,
                activePackageIds = group.ActivePackageIds,
                benchmarks = group.Benchmarks.Select(benchmark => benchmark.Id).ToArray()
            }).ToArray(),
            processes = plan.Processes.Select(process => new
            {
                process.Sequence,
                process.GroupId,
                process.BenchmarkId,
                process.TypeName,
                process.ProjectPath,
                process.AssemblyPath,
                process.AssemblyIdentity,
                process.ModuleVersionId,
                process.AssemblySha256,
                process.StagingOwnerPackageId,
                process.MeasuredSubjectPackageId,
                process.DeterministicSeed,
                process.WorkloadVersion,
                process.ComparisonId,
                process.ProductAbsentControlId,
                methodSelectors = process.MethodSelectors.Select(selector => new
                {
                    selector.Kind,
                    selector.Value,
                    selector.Category
                }).ToArray(),
                throughputCheckpoints = process.ThroughputCheckpoints.Select(checkpoint => new
                {
                    checkpoint.Id,
                    checkpoint.MinimumCount
                }).ToArray(),
                process.EvidenceLens,
                process.Repetition,
                process.WarmUpTicks,
                process.SampleTicks,
                process.GameSpeed,
                activePackageIds = process.ActivePackageIds,
                process.ProcessDirectory,
                process.RawCircinusJsonPath,
                process.NormalizedJsonPath,
                process.CsvReportPath,
                process.MarkdownReportPath
            }).ToArray(),
            reports = new
            {
                plan.AggregateJsonPath,
                plan.AggregateCsvPath,
                plan.SummaryMarkdownPath
            }
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

    private static void ClearLeaseFile(string path)
    {
        if (File.Exists(path)) File.Delete(path);
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
            AllowMultipleArgumentsPerToken = true
        };

        public Option<string> PackageIdFile { get; } = new("--package-id-file")
        {
            Description = "UTF-8 file containing one resolvable package ID per nonblank line."
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
            command.Options.Add(PackageIdFile);
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

            var packageIdFile = result.GetValue(PackageIdFile);
            var filePackageIds = string.IsNullOrWhiteSpace(packageIdFile)
                ? Array.Empty<string>()
                : File.ReadLines(RequiredExistingFile(packageIdFile, "package ID file")).ToArray();
            var packageIds = (result.GetValue(PackageIds) ?? Array.Empty<string>())
                .Concat(filePackageIds)
                .Select(value => value?.Trim().ToLowerInvariant() ?? string.Empty)
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (packageIds.Length == 0)
            {
                throw new ArgumentException(
                    "At least one --package-id or a non-empty --package-id-file is required.");
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
