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
        root.Subcommands.Add(CreatePerformanceBaselineCommand());
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
                var leases = EndToEndStageTransaction.PublishAll(
                    plan.OwnerStages,
                    (owner, prepared) => publisher.Publish(owner, prepared),
                    publisher.TryCleanup,
                    staged => WriteLeaseFile(leasePath, staged),
                    () => ClearLeaseFile(leasePath));

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
            Description = "Root for raw stochastic repetitions, normalized data, CSV, Markdown, and aggregate report paths.",
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
        var diagnosticProfiler = new Option<string?>("--diagnostic-profiler")
        {
            Description = "Optional noncanonical diagnostic profiler; currently only 'dpa'."
        };
        var diagnosticSelector = new Option<string?>("--diagnostic-selector")
        {
            Description = "Exact outer method selector required by the DPA internal-call diagnostic."
        };
        var command = new Command(
            "performance-plan",
            "Build marked performance fixtures and print fresh-process benchmark/repetition plans without staging or launching.");
        common.AddTo(command);
        command.Options.Add(artifactRoot);
        command.Options.Add(warmUpTicks);
        command.Options.Add(sampleTicks);
        command.Options.Add(repetitions);
        command.Options.Add(diagnosticProfiler);
        command.Options.Add(diagnosticSelector);
        command.Options.Add(benchmarkIds);
        command.Options.Add(groupIds);
        command.SetAction(parseResult => Execute(parseResult, () =>
        {
            var input = common.Read(parseResult);
            var projects = PerformanceProjectDiscovery.Discover(input.RepositoryRoot);
            var candidates = new EndToEndProjectBuilder().BuildPerformance(
                projects,
                input.Configuration,
                TimeSpan.FromSeconds(input.BuildTimeoutSeconds));
            var requestedBenchmarks = parseResult.GetValue(benchmarkIds) ?? Array.Empty<string>();
            var requestedGroups = parseResult.GetValue(groupIds) ?? Array.Empty<string>();
            candidates = PerformanceDiscoveryValidator.SelectForFilters(
                    candidates,
                    requestedBenchmarks,
                    requestedGroups)
                .ToArray();
            var profiler = ReadProfiler(parseResult.GetValue(diagnosticProfiler));
            var discovery = PerformanceDiscoveryValidator.ValidateAndGroup(
                candidates,
                input.PackageIds,
                canonicalCircinusIsDeclarationOnly: profiler == PerformanceProfilerMode.DpaDiagnostic);
            var plan = PerformanceRunPlanBuilder.Create(
                discovery,
                requestedBenchmarks,
                requestedGroups,
                parseResult.GetValue(warmUpTicks),
                parseResult.GetValue(sampleTicks),
                parseResult.GetValue(repetitions),
                RequiredPath(parseResult.GetValue(artifactRoot), "artifact root"),
                profiler,
                parseResult.GetValue(diagnosticSelector));
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
        var diagnosticProfiler = new Option<string?>("--diagnostic-profiler")
        {
            Description = "Optional noncanonical diagnostic profiler; currently only 'dpa'."
        };
        var diagnosticSelector = new Option<string?>("--diagnostic-selector")
        {
            Description = "Exact outer method selector required by the DPA internal-call diagnostic."
        };
        var benchmarkIds = new Option<string[]>("--benchmark-id")
        {
            Description = "Exact benchmark ID filter for staged bundles.",
            AllowMultipleArgumentsPerToken = true
        };
        var groupIds = new Option<string[]>("--group-id")
        {
            Description = "Exact performance group ID filter for staged bundles.",
            AllowMultipleArgumentsPerToken = true
        };
        var projectPaths = new Option<string[]>("--project-path")
        {
            Description = "Exact performance fixture project allowlist selected by the prior run plan.",
            AllowMultipleArgumentsPerToken = true,
            Required = true
        };
        var command = new Command(
            "performance-stage",
            "Build marked performance fixtures and atomically publish their exact manifests for one run.");
        common.AddTo(command);
        command.Options.Add(leaseFile);
        command.Options.Add(diagnosticProfiler);
        command.Options.Add(diagnosticSelector);
        command.Options.Add(benchmarkIds);
        command.Options.Add(groupIds);
        command.Options.Add(projectPaths);
        command.SetAction(parseResult => Execute(parseResult, () =>
        {
            var input = common.Read(parseResult);
            var projects = PerformanceProjectDiscovery.SelectExactProjects(
                PerformanceProjectDiscovery.Discover(input.RepositoryRoot),
                parseResult.GetValue(projectPaths) ?? Array.Empty<string>());
            var candidates = new EndToEndProjectBuilder().BuildPerformance(
                projects,
                input.Configuration,
                TimeSpan.FromSeconds(input.BuildTimeoutSeconds));
            candidates = PerformanceDiscoveryValidator.SelectForFilters(
                    candidates,
                    parseResult.GetValue(benchmarkIds) ?? Array.Empty<string>(),
                    parseResult.GetValue(groupIds) ?? Array.Empty<string>())
                .ToArray();
            var plan = PerformanceBundlePlanner.Create(
                candidates,
                input.PackageIds,
                input.ModsRoot,
                input.RimWorldVersion,
                ReadProfiler(parseResult.GetValue(diagnosticProfiler)),
                parseResult.GetValue(diagnosticSelector));
            var publisher = new EndToEndStagePublisher();
            var leasePath = RequiredPath(parseResult.GetValue(leaseFile), "lease file");
            var leases = EndToEndStageTransaction.PublishAll(
                plan.OwnerStages,
                (owner, prepared) => publisher.Publish(owner, prepared),
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

    private static Command CreatePerformanceBaselineCommand()
    {
        var currentSnapshot = new Option<string>("--current-snapshot")
        {
            Description = "Canonical current performance snapshot JSON.",
            Required = true
        };
        var baselineDirectory = new Option<string>("--baseline-directory")
        {
            Description = "Directory containing reviewed *.accepted.json baseline files."
        };
        var policy = new Option<string>("--policy")
        {
            Description = "Tracked per-metric threshold policy JSON."
        };
        var report = new Option<string>("--report")
        {
            Description = "Machine-readable comparison report path."
        };
        var candidateOutput = new Option<string>("--candidate-output")
        {
            Description = "Explicit non-overwriting candidate output path; no accepted comparison is performed."
        };
        var informationalCrossVersion = new Option<bool>("--informational-cross-version")
        {
            Description = "Report version/build/hardware-only drift without applying thresholds or failing."
        };
        var output = OutputOption();
        var command = new Command(
            "performance-baseline",
            "Compare one canonical performance snapshot to reviewed compatible baselines or create a review candidate.");
        command.Options.Add(currentSnapshot);
        command.Options.Add(baselineDirectory);
        command.Options.Add(policy);
        command.Options.Add(report);
        command.Options.Add(candidateOutput);
        command.Options.Add(informationalCrossVersion);
        command.Options.Add(output);
        command.SetAction(parseResult => Execute(parseResult, () =>
        {
            var currentPath = RequiredExistingFile(parseResult.GetValue(currentSnapshot), "current snapshot");
            var current = ReadJson<PerformanceBaselineSnapshot>(currentPath, "current snapshot");
            var candidatePath = parseResult.GetValue(candidateOutput);
            if (!string.IsNullOrWhiteSpace(candidatePath))
            {
                if (!string.IsNullOrWhiteSpace(parseResult.GetValue(baselineDirectory)) ||
                    !string.IsNullOrWhiteSpace(parseResult.GetValue(policy)) ||
                    !string.IsNullOrWhiteSpace(parseResult.GetValue(report)))
                    throw new ArgumentException("Candidate creation cannot be combined with baseline comparison options.");
                var destination = RequiredPath(candidatePath, "candidate output");
                PerformanceBaselineStore.WriteCandidate(destination, current, DateTimeOffset.UtcNow);
                Write(parseResult, parseResult.GetValue(output), new
                {
                    status = "candidate-created",
                    candidate = destination,
                    acceptedBaselineOverwritten = false
                });
                return 0;
            }

            var baselineRoot = RequiredExistingDirectory(parseResult.GetValue(baselineDirectory), "baseline directory");
            var policyPath = RequiredExistingFile(parseResult.GetValue(policy), "threshold policy");
            var reportPath = RequiredPath(parseResult.GetValue(report), "comparison report");
            var baselines = Directory.GetFiles(baselineRoot, "*.accepted.json", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => ReadJson<PerformanceBaselineSnapshot>(path, "accepted baseline"))
                .ToArray();
            var thresholdPolicy = ReadJson<PerformanceThresholdPolicy>(policyPath, "threshold policy");
            var comparison = PerformanceBaselineComparer.Compare(
                current,
                baselines,
                thresholdPolicy,
                parseResult.GetValue(informationalCrossVersion));
            WriteJsonFile(reportPath, comparison);
            var informationOnly = comparison.Passed && comparison.Comparisons.Any(item =>
                string.Equals(item.Status, "informational-cross-version", StringComparison.Ordinal));
            Write(parseResult, parseResult.GetValue(output), new
            {
                status = !comparison.Passed ? "regression" : informationOnly ? "informational" : "passed",
                comparison.Passed,
                report = reportPath,
                comparisonCount = comparison.Comparisons.Count,
                failureCount = comparison.Failures.Count
            });
            return comparison.Passed ? 0 : 1;
        }));
        return command;
    }

    private static T ReadJson<T>(string path, string label)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), new JsonSerializerOptions(JsonOptions)
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new ArgumentException($"The {label} contains no JSON document.");
        }
        catch (JsonException exception)
        {
            throw new ArgumentException($"The {label} is invalid JSON: {exception.Message}");
        }
    }

    private static void WriteJsonFile(string path, object value)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(path, JsonSerializer.Serialize(value, JsonOptions) + Environment.NewLine);
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
        var projects = EndToEndProjectDiscovery.SelectExactProjects(
            EndToEndProjectDiscovery.Discover(input.RepositoryRoot),
            input.ProjectPaths);
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
                process.MaxWallClockSeconds,
                process.GameSpeed,
                profiler = process.Profiler.ToString(),
                process.DiagnosticSelector,
                activePackageIds = process.ActivePackageIds,
                process.ProcessDirectory,
                process.RawCircinusJsonPath,
                process.RawDiagnosticJsonPath,
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

    private static PerformanceProfilerMode ReadProfiler(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return PerformanceProfilerMode.Circinus;
        if (StringComparer.OrdinalIgnoreCase.Equals(value.Trim(), "dpa"))
            return PerformanceProfilerMode.DpaDiagnostic;
        throw new ArgumentException("Diagnostic profiler must be 'dpa'.");
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

        public Option<string[]> ProjectPaths { get; } = new("--project-path")
        {
            Description = "Optional exact E2E fixture project allowlist.",
            AllowMultipleArgumentsPerToken = true
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
            command.Options.Add(ProjectPaths);
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
                (result.GetValue(ProjectPaths) ?? Array.Empty<string>()).Select(Path.GetFullPath).ToArray(),
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
        IReadOnlyList<string> ProjectPaths,
        int BuildTimeoutSeconds);

    private sealed record LeaseRecord(
        string DestinationDirectory,
        string OwnerPackageId,
        string RimWorldVersion,
        string TransactionId,
        EndToEndStageLeaseState State)
    {
        public static LeaseRecord From(EndToEndStageLease lease) => new(
            lease.DestinationDirectory,
            lease.OwnerPackageId,
            lease.RimWorldVersion,
            lease.TransactionId,
            lease.State);

        public EndToEndStageLease ToLease() => new(
            RequiredPath(DestinationDirectory, "lease destination"),
            CommonOptions.RequiredToken(OwnerPackageId, "lease owner package ID"),
            CommonOptions.RequiredToken(RimWorldVersion, "lease RimWorld version"),
            CommonOptions.RequiredToken(TransactionId, "lease transaction ID"),
            Enum.IsDefined(typeof(EndToEndStageLeaseState), State)
                ? State
                : throw new EndToEndStageException("The lease state is invalid."));
    }
}
