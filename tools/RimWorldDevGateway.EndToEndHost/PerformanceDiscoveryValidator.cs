using System.Security.Cryptography;
using System.Text;

namespace RimWorldDevGateway.EndToEndHost;

public static class PerformanceDiscoveryValidator
{
    public const string HarmonyPackageId = "brrainz.harmony";
    public const string CorePackageId = "ludeon.rimworld";
    public const string CircinusPackageId = "astryl.circinus";
    public const string DpaPackageId = "dubwise.dubsperformanceanalyzer.steam";
    public const string GatewayPackageId = "fumblesneeze.rimworlddevgateway";
    public const int MaximumBenchmarks = 1024;
    public const int MaximumActivePackages = 64;
    public const int MaximumPackageIdCharacters = 128;
    public const int MaximumIdentityCharacters = 256;
    public const int MaximumSelectorCharacters = 1024;
    public const int MaximumMethodSelectors = 256;
    public const int MaximumThroughputCheckpoints = 64;
    public const int ExactMethodSelectorKind = 3;

    public static IReadOnlyList<PerformanceAssemblyCandidate> SelectForFilters(
        IEnumerable<PerformanceAssemblyCandidate> assemblyCandidates,
        IEnumerable<string> benchmarkIds,
        IEnumerable<string> groupIds)
    {
        if (assemblyCandidates is null) throw new ArgumentNullException(nameof(assemblyCandidates));
        if (benchmarkIds is null) throw new ArgumentNullException(nameof(benchmarkIds));
        if (groupIds is null) throw new ArgumentNullException(nameof(groupIds));
        var candidates = MaterializeCandidates(assemblyCandidates);
        var benchmarks = NormalizeFilters(benchmarkIds, "benchmark");
        var groups = NormalizeFilters(groupIds, "group");
        if (benchmarks.Count == 0 && groups.Count == 0) return candidates;

        var declarations = candidates
            .SelectMany(candidate => candidate.Metadata.Declarations.Select(declaration => (candidate, declaration)))
            .ToArray();
        var selected = declarations.Where(item =>
                (benchmarks.Count == 0 || benchmarks.Contains(item.declaration.Id.Trim())) &&
                (groups.Count == 0 || groups.Contains(GroupId(item.declaration.ActivePackageIds))))
            .ToArray();
        if (selected.Length == 0)
            throw new EndToEndDiscoveryException(
                "The performance filters selected zero benchmark declarations before package validation.");

        var comparisonIds = selected.Select(item => item.declaration.ComparisonId?.Trim())
            .Where(value => !string.IsNullOrEmpty(value))
            .ToHashSet(StringComparer.Ordinal);
        var included = declarations.Where(item => selected.Contains(item) ||
                                                  comparisonIds.Contains(item.declaration.ComparisonId?.Trim()))
            .ToList();
        var controlIds = included.Select(item => item.declaration.ProductAbsentControlId?.Trim())
            .Where(value => !string.IsNullOrEmpty(value))
            .ToHashSet(StringComparer.Ordinal);
        included.AddRange(declarations.Where(item => controlIds.Contains(item.declaration.Id.Trim())));
        var includedDeclarations = included.Select(item => item.declaration).ToHashSet();

        return candidates.Select(candidate =>
            {
                var retained = candidate.Metadata.Declarations
                    .Where(includedDeclarations.Contains)
                    .ToArray();
                if (retained.Length == 0) return null;
                return new PerformanceAssemblyCandidate(
                    candidate.ProjectPath,
                    candidate.ExpectedOwnerPackageId,
                    candidate.ExpectedAssemblyName,
                    new PerformanceAssemblyMetadata(candidate.Metadata.Assembly, retained));
            })
            .Where(candidate => candidate is not null)
            .Cast<PerformanceAssemblyCandidate>()
            .ToArray();
    }

    public static PerformanceDiscoveryResult ValidateAndGroup(
        IEnumerable<PerformanceAssemblyCandidate> assemblyCandidates,
        IEnumerable<string> resolvablePackageIds,
        bool requireResolvedControls = true,
        bool canonicalCircinusIsDeclarationOnly = false)
    {
        if (assemblyCandidates is null) throw new ArgumentNullException(nameof(assemblyCandidates));
        if (resolvablePackageIds is null) throw new ArgumentNullException(nameof(resolvablePackageIds));

        var candidates = MaterializeCandidates(assemblyCandidates);
        if (candidates.Length == 0)
        {
            throw new EndToEndDiscoveryException("Performance discovery selected zero marked assemblies.");
        }

        var packageCatalog = new HashSet<string>(
            resolvablePackageIds.Where(value => !string.IsNullOrWhiteSpace(value)).Select(Normalize),
            StringComparer.OrdinalIgnoreCase);
        var errors = new List<string>();
        var discovered = new List<PerformanceDiscoveredBenchmark>();
        var totalDeclarations = 0;
        foreach (var candidate in candidates.OrderBy(item => item.ProjectPath, StringComparer.OrdinalIgnoreCase))
        {
            var declarationCount = candidate.Metadata.Declarations.Count;
            if (declarationCount > MaximumBenchmarks - totalDeclarations)
            {
                throw new EndToEndDiscoveryException(
                    $"Performance discovery exceeds the published {MaximumBenchmarks} total performance declarations ceiling.");
            }

            totalDeclarations += declarationCount;
            ValidateAssembly(
                candidate,
                packageCatalog,
                errors,
                discovered,
                canonicalCircinusIsDeclarationOnly);
        }

        foreach (var duplicate in discovered.GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            errors.Add($"duplicate benchmark ID '{duplicate.Key}' is declared by: " +
                       string.Join(", ", duplicate.Select(item => item.TypeName).OrderBy(value => value, StringComparer.Ordinal)));
        }

        ValidateLensFamilies(discovered, errors);
        if (requireResolvedControls)
        {
            ValidateControls(discovered, errors);
        }

        if (errors.Count > 0)
        {
            throw new EndToEndDiscoveryException(
                "Performance metadata discovery failed:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(error => "- " + error)));
        }

        var groups = discovered
            .GroupBy(item => PackageSequenceKey(item.ActivePackageIds), StringComparer.Ordinal)
            .Select(group => new PerformanceTestGroup(
                GroupId(group.First().ActivePackageIds),
                group.First().ActivePackageIds,
                group.OrderBy(item => item.Id, StringComparer.Ordinal)))
            .OrderBy(group => group.GroupId, StringComparer.Ordinal)
            .ToArray();
        return new PerformanceDiscoveryResult(groups);
    }

    private static PerformanceAssemblyCandidate[] MaterializeCandidates(
        IEnumerable<PerformanceAssemblyCandidate> candidates)
    {
        var result = new List<PerformanceAssemblyCandidate>();
        using var enumerator = candidates.GetEnumerator();
        while (enumerator.MoveNext())
        {
            if (result.Count == MaximumBenchmarks)
            {
                throw new EndToEndDiscoveryException(
                    $"Performance discovery exceeds the published {MaximumBenchmarks}-candidate-assembly ceiling " +
                    "(the same ceiling as total declarations)."
                );
            }

            result.Add(enumerator.Current ??
                       throw new EndToEndDiscoveryException("Performance discovery contains a null assembly candidate."));
        }

        return result.ToArray();
    }

    private static void ValidateAssembly(
        PerformanceAssemblyCandidate candidate,
        ISet<string> packageCatalog,
        ICollection<string> errors,
        ICollection<PerformanceDiscoveredBenchmark> discovered,
        bool canonicalCircinusIsDeclarationOnly)
    {
        if (!StringComparer.Ordinal.Equals(candidate.ExpectedAssemblyName, candidate.Metadata.AssemblyName))
        {
            errors.Add($"assembly output '{candidate.Metadata.AssemblyName}' does not match expected output " +
                       $"'{candidate.ExpectedAssemblyName}' for project '{candidate.ProjectPath}'");
        }

        if (candidate.Metadata.Declarations.Count == 0)
        {
            errors.Add($"marked project '{candidate.ProjectPath}' contains no compiled performance declarations");
            return;
        }

        if (candidate.Metadata.Declarations.Count > MaximumBenchmarks)
        {
            errors.Add($"marked project '{candidate.ProjectPath}' exceeds {MaximumBenchmarks} performance declarations");
            return;
        }

        var expectedOwner = Normalize(candidate.ExpectedOwnerPackageId);
        foreach (var declaration in candidate.Metadata.Declarations.OrderBy(item => item.TypeName, StringComparer.Ordinal))
        {
            ValidateDeclaration(
                candidate,
                declaration,
                expectedOwner,
                packageCatalog,
                errors,
                canonicalCircinusIsDeclarationOnly);
            discovered.Add(new PerformanceDiscoveredBenchmark(candidate, declaration));
        }
    }

    private static void ValidateDeclaration(
        PerformanceAssemblyCandidate candidate,
        PerformanceMetadataDeclaration declaration,
        string expectedOwner,
        ISet<string> packageCatalog,
        ICollection<string> errors,
        bool canonicalCircinusIsDeclarationOnly)
    {
        var display = string.IsNullOrWhiteSpace(declaration.Id) ? declaration.TypeName : declaration.Id;
        ValidateBoundedIdentity(declaration.Id, "benchmark ID", display, errors);
        if (!declaration.IsConcrete) errors.Add($"'{display}' must be a concrete class");
        if (!declaration.ImplementsContract) errors.Add($"'{display}' must implement IRimWorldPerformanceTest");
        if (declaration.ThroughputCheckpoints.Count != 0 && !declaration.ImplementsThroughputCounter)
            errors.Add($"'{display}' declares throughput checkpoints but must implement " +
                       "IPerformanceThroughputCounter");
        if (!declaration.HasPublicParameterlessConstructor)
            errors.Add($"'{display}' must expose a public parameterless constructor");

        var owner = ValidatePackage(declaration.StagingOwnerPackageId, "staging owner", display, errors);
        var subject = ValidatePackage(declaration.MeasuredSubjectPackageId, "measured subject", display, errors);
        if (!string.IsNullOrEmpty(owner) && !StringComparer.OrdinalIgnoreCase.Equals(owner, expectedOwner))
        {
            errors.Add($"project owner '{candidate.ExpectedOwnerPackageId}' does not match declared owner " +
                       $"'{declaration.StagingOwnerPackageId}' for '{display}'");
        }

        var packages = declaration.ActivePackageIds.Select((value, index) =>
            ValidatePackage(value, $"active package at index {index}", display, errors)).ToArray();
        if (packages.Length > MaximumActivePackages)
            errors.Add($"'{display}' declares more than {MaximumActivePackages} active packages");
        if (packages.Length < 3 ||
            !StringComparer.OrdinalIgnoreCase.Equals(packages.ElementAtOrDefault(0), HarmonyPackageId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(packages.ElementAtOrDefault(1), CorePackageId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(packages.ElementAtOrDefault(2), CircinusPackageId))
        {
            errors.Add($"'{display}' must begin with exact canonical order {HarmonyPackageId}, {CorePackageId}, {CircinusPackageId}");
        }

        var duplicate = packages.Where(value => !string.IsNullOrEmpty(value))
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null) errors.Add($"'{display}' contains duplicate active package '{duplicate.Key}'");
        if (packages.Contains(GatewayPackageId, StringComparer.OrdinalIgnoreCase))
            errors.Add($"'{display}' must not list implicit Gateway package {GatewayPackageId}");
        if (packages.Contains(DpaPackageId, StringComparer.OrdinalIgnoreCase))
            errors.Add($"'{display}' canonical group must not contain DPA package {DpaPackageId}");
        if (!StringComparer.OrdinalIgnoreCase.Equals(owner, GatewayPackageId) &&
            !packages.Contains(owner, StringComparer.OrdinalIgnoreCase))
            errors.Add($"'{display}' active packages do not contain staging owner '{owner}'");

        if (declaration.EvidenceLens is < 0 or > 3)
            errors.Add($"'{display}' has invalid evidence lens {declaration.EvidenceLens}");
        var subjectActive = StringComparer.OrdinalIgnoreCase.Equals(subject, GatewayPackageId) ||
                            packages.Contains(subject, StringComparer.OrdinalIgnoreCase);
        if (declaration.EvidenceLens == 3)
        {
            if (subjectActive || StringComparer.OrdinalIgnoreCase.Equals(owner, subject))
                errors.Add($"'{display}' product-absent control must keep its subject absent and use another owner");
            if (!string.IsNullOrWhiteSpace(declaration.ProductAbsentControlId))
                errors.Add($"'{display}' product-absent control must not reference another product-absent control");
        }
        else if (!subjectActive)
        {
            errors.Add($"'{display}' active packages do not contain measured subject '{subject}'");
        }

        if (declaration.WarmUpTicks < 0 || declaration.SampleTicks <= 0)
            errors.Add($"'{display}' must declare non-negative warm-up and positive sample ticks");
        if (declaration.GameSpeed is < 1 or > 4) errors.Add($"'{display}' has invalid game speed");
        if (declaration.Repetitions <= 0) errors.Add($"'{display}' must declare positive repetitions");
        ValidateBoundedIdentity(declaration.WorkloadVersion, "workload version", display, errors);
        ValidateBoundedIdentity(declaration.ComparisonId, "comparison ID", display, errors);
        if (declaration.ProductAbsentControlId is not null)
            ValidateBoundedIdentity(declaration.ProductAbsentControlId, "product-absent control ID", display, errors);

        ValidateSelectors(declaration, display, errors);
        ValidateCheckpoints(declaration, display, errors);
        foreach (var package in packages.Where(value => !string.IsNullOrEmpty(value)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!packageCatalog.Contains(package) &&
                !(canonicalCircinusIsDeclarationOnly &&
                  StringComparer.OrdinalIgnoreCase.Equals(package, CircinusPackageId)))
                errors.Add($"'{display}' declares unresolvable package '{package}'");
        }
    }

    private static void ValidateSelectors(
        PerformanceMetadataDeclaration declaration,
        string display,
        ICollection<string> errors)
    {
        if (declaration.MethodSelectors.Count > MaximumMethodSelectors)
            errors.Add($"'{display}' exceeds {MaximumMethodSelectors} method selectors");
        foreach (var selector in declaration.MethodSelectors)
        {
            if (selector.Kind is < 0 or > 5) errors.Add($"'{display}' has invalid method selector kind");
            ValidateBounded(selector.Value, MaximumSelectorCharacters, "method selector value", display, errors);
            ValidateBoundedIdentity(selector.Category, "method selector category", display, errors);
        }

        var duplicate = declaration.MethodSelectors.GroupBy(
            item => item.Kind + ":" + LengthPrefixed(item.Value), StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null) errors.Add($"'{display}' contains a duplicate method selector");
    }

    private static void ValidateCheckpoints(
        PerformanceMetadataDeclaration declaration,
        string display,
        ICollection<string> errors)
    {
        if (declaration.ThroughputCheckpoints.Count > MaximumThroughputCheckpoints)
            errors.Add($"'{display}' exceeds {MaximumThroughputCheckpoints} throughput checkpoints");
        foreach (var checkpoint in declaration.ThroughputCheckpoints)
        {
            ValidateBoundedIdentity(checkpoint.Id, "throughput checkpoint ID", display, errors);
            if (checkpoint.MinimumCount <= 0)
                errors.Add($"'{display}' has non-positive throughput checkpoint '{checkpoint.Id}'");
        }

        var duplicate = declaration.ThroughputCheckpoints.GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null) errors.Add($"'{display}' contains duplicate throughput checkpoint '{duplicate.Key}'");
    }

    private static void ValidateLensFamilies(
        IReadOnlyList<PerformanceDiscoveredBenchmark> benchmarks,
        ICollection<string> errors)
    {
        foreach (var family in benchmarks.Where(item => item.EvidenceLens != 3).GroupBy(
                     item => PackageSequenceKey(item.ActivePackageIds) + LengthPrefixed(item.ComparisonId),
                     StringComparer.Ordinal))
        {
            var items = family.ToArray();
            foreach (var lens in new[] { 0, 1, 2 })
            {
                if (items.Count(item => item.EvidenceLens == lens) != 1)
                    errors.Add($"performance comparison '{items[0].ComparisonId}' must declare exactly one evidence lens {lens}");
            }

            var baseline = items.FirstOrDefault(item => item.EvidenceLens == 0);
            if (baseline is null) continue;
            foreach (var companion in items.Where(item => !ReferenceEquals(item, baseline)))
            {
                if (!StringComparer.OrdinalIgnoreCase.Equals(baseline.StagingOwnerPackageId, companion.StagingOwnerPackageId) ||
                    !StringComparer.OrdinalIgnoreCase.Equals(baseline.MeasuredSubjectPackageId, companion.MeasuredSubjectPackageId) ||
                    baseline.DeterministicSeed != companion.DeterministicSeed ||
                    !string.Equals(baseline.WorkloadVersion, companion.WorkloadVersion, StringComparison.Ordinal) ||
                    baseline.WarmUpTicks != companion.WarmUpTicks || baseline.SampleTicks != companion.SampleTicks ||
                    baseline.GameSpeed != companion.GameSpeed || baseline.Repetitions != companion.Repetitions ||
                    !string.Equals(baseline.ProductAbsentControlId, companion.ProductAbsentControlId, StringComparison.OrdinalIgnoreCase) ||
                    !SelectorKeys(baseline).SequenceEqual(SelectorKeys(companion), StringComparer.Ordinal) ||
                    !CheckpointKeys(baseline).SequenceEqual(CheckpointKeys(companion), StringComparer.Ordinal))
                {
                    errors.Add($"performance comparison '{baseline.ComparisonId}' has incompatible evidence-lens metadata");
                }
            }
        }
    }

    private static void ValidateControls(
        IReadOnlyList<PerformanceDiscoveredBenchmark> benchmarks,
        ICollection<string> errors)
    {
        foreach (var product in benchmarks.Where(item => !string.IsNullOrWhiteSpace(item.ProductAbsentControlId)))
        {
            var controls = benchmarks.Where(item => string.Equals(
                item.Id, product.ProductAbsentControlId, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (controls.Length != 1)
            {
                errors.Add($"benchmark '{product.Id}' resolves {controls.Length} product-absent controls named '{product.ProductAbsentControlId}'");
                continue;
            }

            var control = controls[0];
            var nonSubjectPackages = product.ActivePackageIds.Where(package =>
                !StringComparer.OrdinalIgnoreCase.Equals(package, product.MeasuredSubjectPackageId));
            if (control.EvidenceLens != 3 ||
                !StringComparer.OrdinalIgnoreCase.Equals(control.StagingOwnerPackageId, product.StagingOwnerPackageId) ||
                !StringComparer.OrdinalIgnoreCase.Equals(control.MeasuredSubjectPackageId, product.MeasuredSubjectPackageId) ||
                !nonSubjectPackages.SequenceEqual(control.ActivePackageIds, StringComparer.OrdinalIgnoreCase) ||
                product.DeterministicSeed != control.DeterministicSeed ||
                !string.Equals(product.WorkloadVersion, control.WorkloadVersion, StringComparison.Ordinal) ||
                product.WarmUpTicks != control.WarmUpTicks || product.SampleTicks != control.SampleTicks ||
                product.GameSpeed != control.GameSpeed || product.Repetitions != control.Repetitions ||
                !CheckpointKeys(product).SequenceEqual(CheckpointKeys(control), StringComparer.Ordinal))
            {
                errors.Add($"benchmark '{product.Id}' and control '{control.Id}' are not one semantically compatible owner/workload pair");
            }
        }
    }

    private static IEnumerable<string> SelectorKeys(PerformanceDiscoveredBenchmark item) =>
        item.MethodSelectors.Select(selector => selector.Kind + ":" + LengthPrefixed(selector.Value) + LengthPrefixed(selector.Category));
    private static IEnumerable<string> CheckpointKeys(PerformanceDiscoveredBenchmark item) =>
        item.ThroughputCheckpoints.Select(checkpoint => LengthPrefixed(checkpoint.Id) + checkpoint.MinimumCount);

    private static void ValidateBoundedIdentity(string? value, string field, string display, ICollection<string> errors) =>
        ValidateBounded(value, MaximumIdentityCharacters, field, display, errors);

    private static void ValidateBounded(
        string? value,
        int maximum,
        string field,
        string display,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) errors.Add($"'{display}' has empty {field}");
        else if (value!.Trim().Length > maximum) errors.Add($"'{display}' has {field} longer than {maximum} characters");
    }

    private static string ValidatePackage(
        string? value,
        string field,
        string display,
        ICollection<string> errors)
    {
        var normalized = Normalize(value);
        if (string.IsNullOrEmpty(normalized)) errors.Add($"'{display}' has empty {field} package ID");
        else if (normalized.Length > MaximumPackageIdCharacters)
            errors.Add($"'{display}' has {field} package ID longer than {MaximumPackageIdCharacters} characters");
        else if (normalized.Any(character => !IsPackageCharacter(character)))
            errors.Add($"'{display}' has invalid {field} package ID");
        return normalized;
    }

    private static bool IsPackageCharacter(char value) =>
        value is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '_' or '-';
    private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;
    private static string LengthPrefixed(string value) => value.Length + ":" + value;
    private static string PackageSequenceKey(IEnumerable<string> packages) => string.Concat(packages.Select(LengthPrefixed));
    internal static string GroupId(IEnumerable<string> packages)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(PackageSequenceKey(packages)));
        return "perf-" + Convert.ToHexString(digest.AsSpan(0, 12)).ToLowerInvariant();
    }

    private static HashSet<string> NormalizeFilters(IEnumerable<string> values, string description)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (normalized.Length == 0 || normalized.Length > MaximumIdentityCharacters)
                throw new EndToEndDiscoveryException($"A performance {description} filter is invalid.");
            result.Add(normalized);
        }
        return result;
    }
}
