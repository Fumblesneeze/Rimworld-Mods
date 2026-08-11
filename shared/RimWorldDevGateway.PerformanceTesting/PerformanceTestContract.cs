using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.PerformanceTesting;

public enum PerformanceGameSpeed
{
    Normal = 1,
    Fast = 2,
    Superfast = 3,
    Ultrafast = 4
}

public enum PerformanceEvidenceLens
{
    ProductInstrumented = 0,
    ArmedDisabledWrapper = 1,
    FullyDisarmed = 2,
    ProductAbsentControl = 3
}

public enum PerformanceMethodSelectorKind
{
    HarmonyOwner = 0,
    TickOverrides = 1,
    Type = 2,
    Method = 3,
    CircinusTarget = 4,
    WholeAssembly = 5
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RimWorldPerformanceTestAttribute : Attribute
{
    public RimWorldPerformanceTestAttribute(
        string id,
        string stagingOwnerPackageId,
        string measuredSubjectPackageId,
        params string[] activePackageIds)
    {
        Id = id;
        StagingOwnerPackageId = stagingOwnerPackageId;
        MeasuredSubjectPackageId = measuredSubjectPackageId;
        ActivePackageIds = activePackageIds ?? Array.Empty<string>();
    }

    public string Id { get; }

    public string StagingOwnerPackageId { get; }

    public string MeasuredSubjectPackageId { get; }

    public string[] ActivePackageIds { get; }

    public int DeterministicSeed { get; set; } = 1;

    public string WorkloadVersion { get; set; } = "v1";

    public string? ComparisonId { get; set; }

    public int WarmUpTicks { get; set; } = 2_500;

    public int SampleTicks { get; set; } = 12_000;

    public PerformanceGameSpeed GameSpeed { get; set; } = PerformanceGameSpeed.Superfast;

    public int Repetitions { get; set; } = 3;

    public PerformanceEvidenceLens EvidenceLens { get; set; } = PerformanceEvidenceLens.ProductInstrumented;

    public string? ProductAbsentControlId { get; set; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class PerformanceMethodSelectorAttribute : Attribute
{
    public PerformanceMethodSelectorAttribute(
        PerformanceMethodSelectorKind kind,
        string value,
        string category)
    {
        Kind = kind;
        Value = value;
        Category = category;
    }

    public PerformanceMethodSelectorKind Kind { get; }

    public string Value { get; }

    public string Category { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class PerformanceThroughputCheckpointAttribute : Attribute
{
    public PerformanceThroughputCheckpointAttribute(string id, long minimumCount)
    {
        Id = id;
        MinimumCount = minimumCount;
    }

    public string Id { get; }

    public long MinimumCount { get; }
}

public interface IRimWorldPerformanceTest
{
    void Arrange(IEndToEndContext context);

    IEnumerator<EndToEndStep> Execute(IEndToEndContext context);
}

public sealed class PerformanceMethodSelector
{
    internal PerformanceMethodSelector(PerformanceMethodSelectorKind kind, string value, string category)
    {
        Kind = kind;
        Value = value;
        Category = category;
    }

    public PerformanceMethodSelectorKind Kind { get; }

    public string Value { get; }

    public string Category { get; }
}

public sealed class PerformanceThroughputCheckpoint
{
    internal PerformanceThroughputCheckpoint(string id, long minimumCount)
    {
        Id = id;
        MinimumCount = minimumCount;
    }

    public string Id { get; }

    public long MinimumCount { get; }
}

public sealed class PerformanceTestDescriptor
{
    internal PerformanceTestDescriptor(
        Type testType,
        string id,
        string stagingOwnerPackageId,
        string measuredSubjectPackageId,
        IReadOnlyList<string> activePackageIds,
        int deterministicSeed,
        string workloadVersion,
        string comparisonId,
        int warmUpTicks,
        int sampleTicks,
        PerformanceGameSpeed gameSpeed,
        int repetitions,
        PerformanceEvidenceLens evidenceLens,
        string? productAbsentControlId,
        IReadOnlyList<PerformanceMethodSelector> methodSelectors,
        IReadOnlyList<PerformanceThroughputCheckpoint> throughputCheckpoints)
    {
        TestType = testType;
        Id = id;
        StagingOwnerPackageId = stagingOwnerPackageId;
        MeasuredSubjectPackageId = measuredSubjectPackageId;
        ActivePackageIds = activePackageIds;
        DeterministicSeed = deterministicSeed;
        WorkloadVersion = workloadVersion;
        ComparisonId = comparisonId;
        WarmUpTicks = warmUpTicks;
        SampleTicks = sampleTicks;
        GameSpeed = gameSpeed;
        Repetitions = repetitions;
        EvidenceLens = evidenceLens;
        ProductAbsentControlId = productAbsentControlId;
        MethodSelectors = methodSelectors;
        ThroughputCheckpoints = throughputCheckpoints;
        LaunchedPackageIds = Array.AsReadOnly(activePackageIds
            .Concat(new[] { EndToEndTestContract.GatewayPackageId })
            .ToArray());
    }

    public Type TestType { get; }

    public string Id { get; }

    public string StagingOwnerPackageId { get; }

    public string MeasuredSubjectPackageId { get; }

    public IReadOnlyList<string> ActivePackageIds { get; }

    public IReadOnlyList<string> LaunchedPackageIds { get; }

    public int DeterministicSeed { get; }

    public string WorkloadVersion { get; }

    public string ComparisonId { get; }

    public int WarmUpTicks { get; }

    public int SampleTicks { get; }

    public PerformanceGameSpeed GameSpeed { get; }

    public int Repetitions { get; }

    public PerformanceEvidenceLens EvidenceLens { get; }

    public string? ProductAbsentControlId { get; }

    public IReadOnlyList<PerformanceMethodSelector> MethodSelectors { get; }

    public IReadOnlyList<PerformanceThroughputCheckpoint> ThroughputCheckpoints { get; }
}

public sealed class PerformanceTestGroup
{
    internal PerformanceTestGroup(
        string groupId,
        IReadOnlyList<string> activePackageIds,
        IReadOnlyList<PerformanceTestDescriptor> benchmarks)
    {
        GroupId = groupId;
        ActivePackageIds = activePackageIds;
        Benchmarks = benchmarks;
    }

    public string GroupId { get; }

    public IReadOnlyList<string> ActivePackageIds { get; }

    public IReadOnlyList<PerformanceTestDescriptor> Benchmarks { get; }
}

public sealed class PerformanceContractException : Exception
{
    public PerformanceContractException(string message) : base(message)
    {
    }
}

public static class PerformanceTestContract
{
    public const string CircinusPackageId = "astryl.circinus";
    public const string DpaPackageId = "dubwise.dubsperformanceanalyzer.steam";
    public const int MaximumBenchmarks = 1024;
    public const int MaximumActivePackages = 64;
    public const int MaximumPackageIdCharacters = 128;
    public const int MaximumIdentityCharacters = 256;
    public const int MaximumSelectorCharacters = 1024;
    public const int MaximumMethodSelectors = 256;
    public const int MaximumThroughputCheckpoints = 64;

    public static PerformanceTestDescriptor Describe(Type testType)
    {
        if (testType is null)
        {
            throw new ArgumentNullException(nameof(testType));
        }

        ValidateAttributeMetadataCardinality(testType);
        var attributes = testType
            .GetCustomAttributes(typeof(RimWorldPerformanceTestAttribute), inherit: false)
            .Cast<RimWorldPerformanceTestAttribute>()
            .ToArray();
        if (attributes.Length != 1)
        {
            throw Invalid(testType, "must declare exactly one RimWorldPerformanceTest attribute");
        }

        if (!testType.IsClass || testType.IsAbstract)
        {
            throw Invalid(testType, "must be a concrete class");
        }

        if (!typeof(IRimWorldPerformanceTest).IsAssignableFrom(testType))
        {
            throw Invalid(testType, $"must implement {nameof(IRimWorldPerformanceTest)}");
        }

        if (testType.GetConstructor(Type.EmptyTypes) is null)
        {
            throw Invalid(testType, "must expose a public parameterless constructor");
        }

        var attribute = attributes[0];
        var id = RequiredBounded(attribute.Id, MaximumIdentityCharacters, "benchmark ID", testType);
        var owner = Package(attribute.StagingOwnerPackageId, "staging owner package ID", testType);
        var subject = Package(attribute.MeasuredSubjectPackageId, "measured subject package ID", testType);
        var packages = (attribute.ActivePackageIds ?? Array.Empty<string>())
            .Select((value, index) => Package(value, $"active package ID at index {index}", testType))
            .ToArray();

        if (packages.Length == 0)
        {
            throw Invalid(testType, "must declare a non-empty active package sequence");
        }


        if (packages.Length > MaximumActivePackages)
        {
            throw Invalid(testType, $"declares more than {MaximumActivePackages} active packages");
        }

        ValidateLaunchPrefix(packages, testType);
        RejectDuplicatePackages(packages, testType);
        if (packages.Contains(EndToEndTestContract.GatewayPackageId, StringComparer.OrdinalIgnoreCase))
        {
            throw Invalid(testType, "must not list the implicit Gateway package");
        }

        if (!packages.Contains(CircinusPackageId, StringComparer.OrdinalIgnoreCase))
        {
            throw Invalid(testType, $"canonical benchmarks must contain {CircinusPackageId}");
        }

        if (packages.Contains(DpaPackageId, StringComparer.OrdinalIgnoreCase))
        {
            throw Invalid(testType, $"canonical benchmarks must not contain DubsPerformanceAnalyzer package {DpaPackageId}");
        }

        if (!StringComparer.OrdinalIgnoreCase.Equals(owner, EndToEndTestContract.GatewayPackageId) &&
            !packages.Contains(owner, StringComparer.OrdinalIgnoreCase))
        {
            throw Invalid(testType, $"does not contain staging owner package '{owner}'");
        }

        if (!Enum.IsDefined(typeof(PerformanceEvidenceLens), attribute.EvidenceLens))
        {
            throw Invalid(testType, "has an invalid evidence lens");
        }

        var subjectActive =
            StringComparer.OrdinalIgnoreCase.Equals(subject, EndToEndTestContract.GatewayPackageId) ||
            packages.Contains(subject, StringComparer.OrdinalIgnoreCase);
        if (attribute.EvidenceLens == PerformanceEvidenceLens.ProductAbsentControl)
        {
            if (subjectActive || StringComparer.OrdinalIgnoreCase.Equals(owner, subject))
            {
                throw Invalid(testType, "a product-absent control must keep the measured subject absent and use a different staging owner");
            }
        }
        else if (!subjectActive)
        {
            throw Invalid(testType, $"does not contain measured subject package '{subject}'");
        }

        if (attribute.WarmUpTicks < 0 || attribute.SampleTicks <= 0)
        {
            throw Invalid(testType, "must declare non-negative warm-up ticks and positive sample ticks");
        }

        if (attribute.Repetitions <= 0)
        {
            throw Invalid(testType, "must declare positive repetitions");
        }

        if (!Enum.IsDefined(typeof(PerformanceGameSpeed), attribute.GameSpeed))
        {
            throw Invalid(testType, "has an invalid game speed");
        }

        var workloadVersion = Required(attribute.WorkloadVersion, "workload version", testType);
        RequireBounded(workloadVersion, MaximumIdentityCharacters, "workload version", testType);
        var comparisonId = string.IsNullOrWhiteSpace(attribute.ComparisonId)
            ? id
            : attribute.ComparisonId!.Trim();
        RequireBounded(comparisonId, MaximumIdentityCharacters, "comparison ID", testType);
        var controlId = string.IsNullOrWhiteSpace(attribute.ProductAbsentControlId)
            ? null
            : attribute.ProductAbsentControlId!.Trim();
        if (controlId is not null)
        {
            RequireBounded(controlId, MaximumIdentityCharacters, "product-absent control ID", testType);
        }
        if (attribute.EvidenceLens == PerformanceEvidenceLens.ProductAbsentControl && controlId is not null)
        {
            throw Invalid(testType, "a product-absent control cannot point to another product-absent control");
        }

        var selectors = ReadSelectors(testType);
        var checkpoints = ReadCheckpoints(testType);
        return new PerformanceTestDescriptor(
            testType,
            id,
            owner,
            subject,
            Array.AsReadOnly(packages),
            attribute.DeterministicSeed,
            workloadVersion,
            comparisonId,
            attribute.WarmUpTicks,
            attribute.SampleTicks,
            attribute.GameSpeed,
            attribute.Repetitions,
            attribute.EvidenceLens,
            controlId,
            selectors,
            checkpoints);
    }

    public static IReadOnlyList<PerformanceTestGroup> DescribeAndGroup(IEnumerable<Type> testTypes)
    {
        if (testTypes is null)
        {
            throw new ArgumentNullException(nameof(testTypes));
        }

        var descriptors = MaterializeTypes(testTypes).Select(Describe).ToArray();
        if (descriptors.Length == 0)
        {
            throw new PerformanceContractException("Performance discovery selected zero benchmarks.");
        }
        var duplicate = descriptors.GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new PerformanceContractException($"Duplicate benchmark ID '{duplicate.Key}'.");
        }

        ValidateProductAbsentControls(descriptors);
        ValidateEvidenceLensFamilies(descriptors);

        return descriptors
            .GroupBy(item => PackageSequenceKey(item.ActivePackageIds), StringComparer.Ordinal)
            .Select(group =>
            {
                var benchmarks = group.OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
                var packages = benchmarks[0].ActivePackageIds.ToArray();
                return new PerformanceTestGroup(
                    GroupId(packages),
                    Array.AsReadOnly(packages),
                    Array.AsReadOnly(benchmarks));
            })
            .OrderBy(group => group.GroupId, StringComparer.Ordinal)
            .ToArray();
    }

    private static Type[] MaterializeTypes(IEnumerable<Type> testTypes)
    {
        var result = new List<Type>();
        using var enumerator = testTypes.GetEnumerator();
        while (enumerator.MoveNext())
        {
            if (result.Count == MaximumBenchmarks)
            {
                throw new PerformanceContractException(
                    $"Performance discovery exceeds the published {MaximumBenchmarks}-benchmark ceiling.");
            }

            result.Add(enumerator.Current ??
                       throw new PerformanceContractException("Performance discovery contains a null benchmark type."));
        }

        return result.ToArray();
    }

    private static void ValidateEvidenceLensFamilies(IReadOnlyList<PerformanceTestDescriptor> descriptors)
    {
        var productDescriptors = descriptors
            .Where(item => item.EvidenceLens != PerformanceEvidenceLens.ProductAbsentControl)
            .ToArray();
        foreach (var family in productDescriptors.GroupBy(
                     item => PackageSequenceKey(item.ActivePackageIds) + "|" + LengthPrefixed(item.ComparisonId),
                     StringComparer.Ordinal))
        {
            var ordered = family.OrderBy(item => item.EvidenceLens).ToArray();
            foreach (var required in new[]
                     {
                         PerformanceEvidenceLens.ProductInstrumented,
                         PerformanceEvidenceLens.ArmedDisabledWrapper,
                         PerformanceEvidenceLens.FullyDisarmed
                     })
            {
                var matches = ordered.Where(item => item.EvidenceLens == required).ToArray();
                if (matches.Length != 1)
                {
                    throw new PerformanceContractException(
                        $"Performance comparison '{ordered[0].ComparisonId}' must declare exactly one {required} evidence lens; observed {matches.Length}.");
                }
            }

            var baseline = ordered.Single(item => item.EvidenceLens == PerformanceEvidenceLens.ProductInstrumented);
            foreach (var companion in ordered.Where(item => !ReferenceEquals(item, baseline)))
            {
                if (!StringComparer.OrdinalIgnoreCase.Equals(
                        baseline.StagingOwnerPackageId,
                        companion.StagingOwnerPackageId) ||
                    !StringComparer.OrdinalIgnoreCase.Equals(
                        baseline.MeasuredSubjectPackageId,
                        companion.MeasuredSubjectPackageId) ||
                    baseline.DeterministicSeed != companion.DeterministicSeed ||
                    !string.Equals(baseline.WorkloadVersion, companion.WorkloadVersion, StringComparison.Ordinal) ||
                    baseline.WarmUpTicks != companion.WarmUpTicks ||
                    baseline.SampleTicks != companion.SampleTicks ||
                    baseline.GameSpeed != companion.GameSpeed ||
                    baseline.Repetitions != companion.Repetitions ||
                    !string.Equals(
                        baseline.ProductAbsentControlId,
                        companion.ProductAbsentControlId,
                        StringComparison.OrdinalIgnoreCase) ||
                    !SelectorKeys(baseline).SequenceEqual(SelectorKeys(companion), StringComparer.Ordinal) ||
                    !CheckpointKeys(baseline).SequenceEqual(CheckpointKeys(companion), StringComparer.Ordinal))
                {
                    throw new PerformanceContractException(
                        $"Performance comparison '{baseline.ComparisonId}' has incompatible workload, selector, throughput, or control metadata between its evidence lenses.");
                }
            }
        }
    }

    private static void ValidateProductAbsentControls(IReadOnlyList<PerformanceTestDescriptor> descriptors)
    {
        foreach (var product in descriptors.Where(item => item.ProductAbsentControlId is not null))
        {
            var controls = descriptors.Where(item => string.Equals(
                    item.Id,
                    product.ProductAbsentControlId,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (controls.Length == 0)
            {
                throw new PerformanceContractException(
                    $"Benchmark '{product.Id}' declares missing product-absent control '{product.ProductAbsentControlId}'.");
            }

            if (controls.Length != 1)
            {
                throw new PerformanceContractException(
                    $"Benchmark '{product.Id}' resolves ambiguous product-absent control '{product.ProductAbsentControlId}'.");
            }

            var control = controls[0];
            if (control.EvidenceLens != PerformanceEvidenceLens.ProductAbsentControl ||
                !StringComparer.OrdinalIgnoreCase.Equals(
                    control.MeasuredSubjectPackageId,
                    product.MeasuredSubjectPackageId))
            {
                throw new PerformanceContractException(
                    $"Benchmark '{product.Id}' references '{control.Id}', which is not its product-absent control for measured subject '{product.MeasuredSubjectPackageId}'.");
            }


            if (!StringComparer.OrdinalIgnoreCase.Equals(
                    control.StagingOwnerPackageId,
                    product.StagingOwnerPackageId))
            {
                throw new PerformanceContractException(
                    $"Benchmark '{product.Id}' and product-absent control '{control.Id}' must share one non-subject staging owner so their workload implementation is identical.");
            }

            var productPackages = product.ActivePackageIds
                .Where(packageId => !StringComparer.OrdinalIgnoreCase.Equals(
                    packageId,
                    product.MeasuredSubjectPackageId));
            if (!productPackages.SequenceEqual(control.ActivePackageIds, StringComparer.OrdinalIgnoreCase))
            {
                throw new PerformanceContractException(
                    $"Benchmark '{product.Id}' and product-absent control '{control.Id}' do not retain the same exact non-subject package order.");
            }

            if (product.DeterministicSeed != control.DeterministicSeed ||
                !string.Equals(product.WorkloadVersion, control.WorkloadVersion, StringComparison.Ordinal) ||
                product.WarmUpTicks != control.WarmUpTicks ||
                product.SampleTicks != control.SampleTicks ||
                product.GameSpeed != control.GameSpeed ||
                product.Repetitions != control.Repetitions)
            {
                throw new PerformanceContractException(
                    $"Benchmark '{product.Id}' and product-absent control '{control.Id}' have incompatible workload or sampling identity.");
            }

            var productCheckpoints = CheckpointKeys(product);
            var controlCheckpoints = CheckpointKeys(control);
            if (!productCheckpoints.SequenceEqual(controlCheckpoints, StringComparer.Ordinal))
            {
                throw new PerformanceContractException(
                    $"Benchmark '{product.Id}' and product-absent control '{control.Id}' have incompatible throughput checkpoints.");
            }
        }
    }

    private static IReadOnlyList<PerformanceMethodSelector> ReadSelectors(Type testType)
    {
        var attributes = testType
            .GetCustomAttributes(typeof(PerformanceMethodSelectorAttribute), inherit: false)
            .Cast<PerformanceMethodSelectorAttribute>()
            .ToArray();
        if (attributes.Length > MaximumMethodSelectors)
        {
            throw Invalid(testType, $"declares more than {MaximumMethodSelectors} method selectors");
        }

        var selectors = attributes.Select(attribute =>
        {
            if (!Enum.IsDefined(typeof(PerformanceMethodSelectorKind), attribute.Kind))
            {
                throw Invalid(testType, "has an invalid method selector kind");
            }

            return new PerformanceMethodSelector(
                attribute.Kind,
                RequiredBounded(attribute.Value, MaximumSelectorCharacters, "method selector value", testType),
                RequiredBounded(attribute.Category, MaximumIdentityCharacters, "method selector category", testType));
        }).OrderBy(item => item.Kind).ThenBy(item => item.Value, StringComparer.Ordinal).ToArray();

        var duplicate = selectors.GroupBy(
                item => ((int)item.Kind).ToString() + ":" + LengthPrefixed(item.Value),
                StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw Invalid(testType, "contains a duplicate method selector");
        }

        return Array.AsReadOnly(selectors);
    }

    private static IReadOnlyList<PerformanceThroughputCheckpoint> ReadCheckpoints(Type testType)
    {
        var attributes = testType
            .GetCustomAttributes(typeof(PerformanceThroughputCheckpointAttribute), inherit: false)
            .Cast<PerformanceThroughputCheckpointAttribute>()
            .ToArray();
        if (attributes.Length > MaximumThroughputCheckpoints)
        {
            throw Invalid(testType, $"declares more than {MaximumThroughputCheckpoints} throughput checkpoints");
        }

        var checkpoints = attributes.Select(attribute =>
        {
            if (attribute.MinimumCount <= 0)
            {
                throw Invalid(testType, "has a throughput checkpoint with a non-positive minimum count");
            }

            return new PerformanceThroughputCheckpoint(
                RequiredBounded(attribute.Id, MaximumIdentityCharacters, "throughput checkpoint ID", testType),
                attribute.MinimumCount);
        }).OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
        var duplicate = checkpoints.GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw Invalid(testType, $"contains duplicate throughput checkpoint '{duplicate.Key}'");
        }

        return Array.AsReadOnly(checkpoints);
    }

    private static void ValidateAttributeMetadataCardinality(Type testType)
    {
        var metadata = testType.GetCustomAttributesData();
        var benchmarkAttributes = metadata.Where(item =>
                item.AttributeType == typeof(RimWorldPerformanceTestAttribute))
            .ToArray();
        if (benchmarkAttributes.Length == 1 && benchmarkAttributes[0].ConstructorArguments.Count == 4)
        {
            var packages = benchmarkAttributes[0].ConstructorArguments[3].Value as
                IList<CustomAttributeTypedArgument>;
            if (packages is not null && packages.Count > MaximumActivePackages)
            {
                throw Invalid(testType, $"declares more than {MaximumActivePackages} active packages");
            }
        }

        var selectorCount = metadata.Count(item =>
            item.AttributeType == typeof(PerformanceMethodSelectorAttribute));
        if (selectorCount > MaximumMethodSelectors)
        {
            throw Invalid(testType, $"declares more than {MaximumMethodSelectors} method selectors");
        }

        var checkpointCount = metadata.Count(item =>
            item.AttributeType == typeof(PerformanceThroughputCheckpointAttribute));
        if (checkpointCount > MaximumThroughputCheckpoints)
        {
            throw Invalid(testType, $"declares more than {MaximumThroughputCheckpoints} throughput checkpoints");
        }
    }

    private static void ValidateLaunchPrefix(IReadOnlyList<string> packages, Type testType)
    {
        if (packages.Count < 3 ||
            !StringComparer.OrdinalIgnoreCase.Equals(packages[0], EndToEndTestContract.HarmonyPackageId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(packages[1], EndToEndTestContract.CorePackageId) ||
            !StringComparer.OrdinalIgnoreCase.Equals(packages[2], CircinusPackageId))
        {
            throw Invalid(
                testType,
                $"must begin with exact canonical order {EndToEndTestContract.HarmonyPackageId}, {EndToEndTestContract.CorePackageId}, {CircinusPackageId}");
        }
    }

    private static void RejectDuplicatePackages(IEnumerable<string> packages, Type testType)
    {
        var duplicate = packages.GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw Invalid(testType, $"contains duplicate active package '{duplicate.Key}'");
        }
    }

    private static string Package(string? value, string field, Type testType)
    {
        var package = RequiredBounded(value, MaximumPackageIdCharacters, field, testType);
        if (package.Any(character => !IsPackageCharacter(character)))
        {
            throw Invalid(testType, $"has invalid {field}; package IDs accept only ASCII letters, digits, '.', '_', and '-'");
        }

        return package.ToLowerInvariant();
    }

    private static string Required(string? value, string field, Type testType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Invalid(testType, $"has an empty {field}");
        }

        return value!.Trim();
    }

    private static string RequiredBounded(string? value, int maximumCharacters, string field, Type testType)
    {
        var result = Required(value, field, testType);
        RequireBounded(result, maximumCharacters, field, testType);
        return result;
    }

    private static void RequireBounded(string value, int maximumCharacters, string field, Type testType)
    {
        if (value.Length > maximumCharacters)
        {
            throw Invalid(testType, $"has a {field} longer than {maximumCharacters} characters");
        }
    }

    private static bool IsPackageCharacter(char value) =>
        value is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '.' or '_' or '-';

    private static IEnumerable<string> SelectorKeys(PerformanceTestDescriptor descriptor) =>
        descriptor.MethodSelectors.Select(item =>
            ((int)item.Kind).ToString() + ":" + LengthPrefixed(item.Value) + LengthPrefixed(item.Category));

    private static IEnumerable<string> CheckpointKeys(PerformanceTestDescriptor descriptor) =>
        descriptor.ThroughputCheckpoints.Select(item => LengthPrefixed(item.Id) + item.MinimumCount);

    private static string PackageSequenceKey(IEnumerable<string> packages) =>
        string.Concat(packages.Select(LengthPrefixed));

    private static string LengthPrefixed(string value) => value.Length + ":" + value;

    private static string GroupId(IEnumerable<string> packages)
    {
        using var sha = SHA256.Create();
        var digest = sha.ComputeHash(Encoding.UTF8.GetBytes(PackageSequenceKey(packages)));
        return "perf-" + string.Concat(digest.Take(12).Select(value => value.ToString("x2")));
    }

    private static PerformanceContractException Invalid(Type type, string reason) =>
        new PerformanceContractException($"Performance benchmark '{type.FullName ?? type.Name}' {reason}.");
}
