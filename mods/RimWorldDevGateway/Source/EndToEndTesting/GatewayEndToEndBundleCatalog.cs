using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using RimWorldDevGateway.Contracts;
using RimWorldDevGateway.EndToEndTesting;
using RimWorldDevGateway.Performance;
using RimWorldDevGateway.PerformanceTesting;

namespace RimWorldDevGateway;

public sealed class GatewayEndToEndBundleCatalog : IGatewayEndToEndBundleInspector
{
    private const string ManifestSuffix = ".e2etests.json";
    private readonly IGatewayEndToEndAssemblyLoader assemblyLoader;

    public GatewayEndToEndBundleCatalog(IGatewayEndToEndAssemblyLoader? assemblyLoader = null)
    {
        this.assemblyLoader = assemblyLoader ?? new GatewayEndToEndAssemblyByteLoader();
    }

    public IReadOnlyList<GatewayEndToEndBundleInspectionResult> InspectAll(
        IEnumerable<GatewayEndToEndManifestCandidate> candidates,
        IReadOnlyList<string> activePackageIds)
    {
        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        return new ReadOnlyCollection<GatewayEndToEndBundleInspectionResult>(
            candidates
                .OrderBy(candidate => candidate.ManifestPath, StringComparer.Ordinal)
                .Select(candidate => Inspect(candidate, activePackageIds))
                .ToArray());
    }

    public GatewayEndToEndBundleInspectionResult Inspect(
        GatewayEndToEndManifestCandidate candidate,
        IReadOnlyList<string> activePackageIds)
    {
        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }

        try
        {
            return InspectGuarded(candidate, ValidateActivePackages(activePackageIds));
        }
        catch (GatewayEndToEndCatalogException exception)
        {
            return GatewayEndToEndBundleInspectionResult.Failed(exception.Code, exception.Message);
        }
        catch
        {
            return GatewayEndToEndBundleInspectionResult.Failed(
                "bundle_inspection_failed",
                "The staged E2E bundle failed during guarded inspection; arbitrary exception text was suppressed.");
        }
    }

    private GatewayEndToEndBundleInspectionResult InspectGuarded(
        GatewayEndToEndManifestCandidate candidate,
        IReadOnlyList<string> activePackageIds)
    {
        var manifestPath = candidate.ManifestPath;
        var manifestFileName = Path.GetFileName(manifestPath);
        if (!manifestFileName.EndsWith(ManifestSuffix, StringComparison.Ordinal) ||
            !File.Exists(manifestPath))
        {
            throw Failure("manifest_missing", "The exact staged E2E manifest is missing.");
        }

        EnsureRegularFile(manifestPath, "manifest_unsafe", "The staged E2E manifest is not a regular file.");
        var directory = Path.GetDirectoryName(manifestPath) ??
            throw Failure("manifest_path_invalid", "The staged E2E manifest has no containing directory.");
        if (!StringComparer.Ordinal.Equals(Path.GetFileName(directory), "DevEndToEndTests"))
        {
            throw Failure("manifest_path_invalid", "The staged E2E manifest is outside DevEndToEndTests.");
        }

        var versionDirectory = Path.GetDirectoryName(directory) ??
            throw Failure("manifest_path_invalid", "The staged E2E manifest has no version directory.");
        var ownerDirectory = Path.GetDirectoryName(versionDirectory) ??
            throw Failure("manifest_path_invalid", "The staged E2E manifest has no owner directory.");
        if (!StringComparer.OrdinalIgnoreCase.Equals(
                Path.GetFileName(ownerDirectory),
                candidate.ContainingPackageId))
        {
            throw Failure("manifest_path_invalid", "The staged E2E manifest owner path does not match its containing mod.");
        }

        EndToEndStageMarker marker;
        EndToEndBundleManifest manifest;
        try
        {
            marker = GatewayContractJson.ReadFile<EndToEndStageMarker>(
                Path.Combine(directory, EndToEndStageMarker.FileName));
            manifest = GatewayContractJson.ReadFile<EndToEndBundleManifest>(manifestPath);
        }
        catch
        {
            throw Failure("manifest_invalid", "The staged E2E marker or manifest is missing or invalid.");
        }

        ValidateMarker(
            marker,
            candidate,
            Path.GetFileName(versionDirectory),
            manifestFileName,
            manifest.Assembly);
        ValidateManifest(manifest, candidate, activePackageIds, manifestFileName);
        var matchingTests = manifest.Tests
            .Where(test => MatchesActiveGroup(test.ActivePackageIds, activePackageIds))
            .OrderBy(test => test.Id, StringComparer.Ordinal)
            .ToArray();
        if (matchingTests.Length == 0)
        {
            return GatewayEndToEndBundleInspectionResult.Skipped();
        }

        var assemblyPath = Path.GetFullPath(Path.Combine(directory, manifest.Assembly));
        if (!StringComparer.OrdinalIgnoreCase.Equals(Path.GetDirectoryName(assemblyPath), directory) ||
            !File.Exists(assemblyPath))
        {
            throw Failure("assembly_missing", "The exact sibling E2E assembly is missing.");
        }

        EnsureRegularFile(assemblyPath, "assembly_unsafe", "The staged E2E assembly is not a regular file.");
        var assemblyBytes = File.ReadAllBytes(assemblyPath);
        if (assemblyBytes.LongLength != manifest.AssemblyLength)
        {
            throw Failure("assembly_length_mismatch", "The staged E2E assembly length differs from discovery.");
        }

        var hash = ComputeSha256(assemblyBytes);
        if (!StringComparer.Ordinal.Equals(hash, manifest.AssemblySha256))
        {
            throw Failure("assembly_hash_mismatch", "The staged E2E assembly hash differs from discovery.");
        }

        Assembly assembly;
        try
        {
            assembly = assemblyLoader.Load(assemblyBytes);
        }
        catch
        {
            throw Failure(
                "assembly_byte_load_failed",
                "The staged E2E assembly could not be byte-loaded; arbitrary exception text was suppressed.");
        }

        if (!StringComparer.Ordinal.Equals(
                GatewayEndToEndAssemblyIdentity.FormatDefinition(assembly.GetName()),
                manifest.AssemblyIdentity))
        {
            throw Failure("assembly_identity_mismatch", "The loaded E2E assembly identity differs from discovery.");
        }

        if (!Guid.TryParse(manifest.ModuleVersionId, out var expectedMvid) ||
            assembly.ManifestModule.ModuleVersionId != expectedMvid)
        {
            throw Failure("assembly_mvid_mismatch", "The loaded E2E assembly MVID differs from discovery.");
        }

        ValidateDependencies(assembly, manifest.Dependencies);
        var compiledTests = ReadCompiledTests(assembly);
        ValidateCompiledManifest(compiledTests, manifest.Tests);
        var compiledById = compiledTests.ToDictionary(test => test.Id, StringComparer.Ordinal);
        var admitted = matchingTests.Select(test =>
        {
            var compiled = compiledById[test.Id];
            var performanceDescriptor = EffectivePerformanceDescriptor(compiled.PerformanceDescriptor, test);
            return performanceDescriptor is null
                ? new GatewayEndToEndRuntimeTestDescriptor(
                    test.Id,
                    test.OwnerPackageId,
                    test.ActivePackageIds,
                    test.TypeName,
                    test.MaxFrames,
                    test.MaxGameTicks,
                    test.MaxWallClockSeconds,
                    compiled.TestType)
                : new GatewayEndToEndRuntimeTestDescriptor(
                    test.Id,
                    test.OwnerPackageId,
                    test.ActivePackageIds,
                    test.TypeName,
                    test.MaxFrames,
                    test.MaxGameTicks,
                    test.MaxWallClockSeconds,
                    compiled.TestType,
                    () => new GatewayPerformanceEndToEndAdapter(performanceDescriptor),
                    performanceDescriptor);
        }).ToArray();

        return GatewayEndToEndBundleInspectionResult.Loaded(
            new GatewayEndToEndAssemblySource(
                candidate.ContainingPackageId,
                manifestPath,
                manifest.AssemblyIdentity,
                manifest.AssemblySha256,
                assembly,
                admitted));
    }

    private static IReadOnlyList<string> ValidateActivePackages(IReadOnlyList<string> activePackageIds)
    {
        if (activePackageIds is null || activePackageIds.Count == 0)
        {
            throw Failure("active_package_list_invalid", "The active package list is empty.");
        }

        var normalized = activePackageIds.Select(Normalize).ToArray();
        if (normalized.Any(string.IsNullOrEmpty) ||
            normalized.Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Length ||
            !StringComparer.OrdinalIgnoreCase.Equals(
                normalized[normalized.Length - 1],
                EndToEndTestContract.GatewayPackageId))
        {
            throw Failure(
                "active_package_list_invalid",
                "The active package list must be unique and end with the Dev Gateway.");
        }

        return normalized;
    }

    private static void ValidateMarker(
        EndToEndStageMarker marker,
        GatewayEndToEndManifestCandidate candidate,
        string rimWorldVersion,
        string manifestFileName,
        string assemblyFileName)
    {
        if (!StringComparer.Ordinal.Equals(marker.Schema, EndToEndStageMarker.SchemaValue) ||
            !StringComparer.OrdinalIgnoreCase.Equals(marker.OwnerPackageId, candidate.ContainingPackageId) ||
            !StringComparer.Ordinal.Equals(marker.RimWorldVersion, rimWorldVersion) ||
            string.IsNullOrWhiteSpace(marker.TransactionId) ||
            marker.Manifests is null ||
            !marker.Manifests.Contains(manifestFileName, StringComparer.Ordinal) ||
            marker.Assemblies is null ||
            !marker.Assemblies.Contains(assemblyFileName, StringComparer.Ordinal))
        {
            throw Failure("stage_marker_mismatch", "The staged E2E ownership marker does not match its bundle.");
        }
    }

    private static void ValidateManifest(
        EndToEndBundleManifest manifest,
        GatewayEndToEndManifestCandidate candidate,
        IReadOnlyList<string> activePackageIds,
        string manifestFileName)
    {
        if (!StringComparer.Ordinal.Equals(manifest.Schema, EndToEndBundleManifest.SchemaValue))
        {
            throw Failure("manifest_schema_mismatch", "The staged E2E manifest schema is unsupported.");
        }

        if (!StringComparer.OrdinalIgnoreCase.Equals(manifest.OwnerPackageId, candidate.ContainingPackageId))
        {
            throw Failure("manifest_owner_mismatch", "The staged E2E manifest owner differs from its containing mod.");
        }

        if (!activePackageIds.Contains(candidate.ContainingPackageId, StringComparer.OrdinalIgnoreCase))
        {
            throw Failure("manifest_owner_inactive", "The staged E2E manifest owner is not active.");
        }

        var manifestBaseName = manifestFileName.Substring(0, manifestFileName.Length - ManifestSuffix.Length);
        if (string.IsNullOrWhiteSpace(manifest.Assembly) ||
            !StringComparer.Ordinal.Equals(manifest.Assembly, manifestBaseName + ".dll") ||
            !StringComparer.Ordinal.Equals(Path.GetFileName(manifest.Assembly), manifest.Assembly))
        {
            throw Failure("manifest_assembly_invalid", "The E2E manifest does not name its exact sibling assembly.");
        }

        if (manifest.Tests is null || manifest.Tests.Length == 0)
        {
            throw Failure("manifest_tests_invalid", "The staged E2E manifest contains no tests.");
        }

        var duplicateId = manifest.Tests
            .GroupBy(test => test.Id ?? string.Empty, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateId is not null)
        {
            throw Failure("manifest_tests_invalid", "The staged E2E manifest contains a duplicate test ID.");
        }

        foreach (var test in manifest.Tests)
        {
            ValidateManifestTest(test, candidate.ContainingPackageId);
        }
    }

    private static void ValidateManifestTest(EndToEndBundleTest test, string containingPackageId)
    {
        var owner = Normalize(test.OwnerPackageId);
        var packages = (test.ActivePackageIds ?? Array.Empty<string>()).Select(Normalize).ToArray();
        var kind = string.IsNullOrWhiteSpace(test.Kind) ? EndToEndBundleTest.EndToEndKind : test.Kind;
        if (string.IsNullOrWhiteSpace(test.Id) ||
            string.IsNullOrWhiteSpace(test.TypeName) ||
            (kind != EndToEndBundleTest.EndToEndKind && kind != EndToEndBundleTest.PerformanceKind) ||
            !StringComparer.OrdinalIgnoreCase.Equals(owner, containingPackageId) ||
            packages.Length == 0 ||
            !EndToEndTestContract.HasValidLaunchPrefix(packages) ||
            packages.Any(string.IsNullOrEmpty) ||
            packages.Distinct(StringComparer.OrdinalIgnoreCase).Count() != packages.Length ||
            packages.Contains(EndToEndTestContract.GatewayPackageId, StringComparer.OrdinalIgnoreCase) ||
            (!StringComparer.OrdinalIgnoreCase.Equals(owner, EndToEndTestContract.GatewayPackageId) &&
             !packages.Contains(owner, StringComparer.OrdinalIgnoreCase)) ||
            test.MaxFrames <= 0 ||
            test.MaxGameTicks <= 0 ||
            test.MaxWallClockSeconds <= 0)
        {
            throw Failure("manifest_tests_invalid", "A staged E2E test declaration is invalid.");
        }

        if (kind == EndToEndBundleTest.PerformanceKind &&
            (string.IsNullOrWhiteSpace(test.MeasuredSubjectPackageId) ||
             string.IsNullOrWhiteSpace(test.WorkloadVersion) ||
             string.IsNullOrWhiteSpace(test.ComparisonId) ||
             test.WarmUpTicks < 0 ||
             test.SampleTicks <= 0 ||
             test.GameSpeed is < 1 or > 4 ||
             test.Repetitions <= 0 ||
             test.EvidenceLens is < 0 or > 3 ||
             test.MethodSelectors is null ||
             test.ThroughputCheckpoints is null))
        {
            throw Failure("manifest_tests_invalid", "A staged performance declaration is incomplete.");
        }
        if (kind == EndToEndBundleTest.PerformanceKind)
        {
            var profiler = string.IsNullOrWhiteSpace(test.PerformanceProfiler)
                ? "circinus"
                : test.PerformanceProfiler!.Trim().ToLowerInvariant();
            var dpa = profiler == "dpa";
            if ((profiler != "circinus" && !dpa) ||
                packages.Length < 3 ||
                (dpa && (!StringComparer.OrdinalIgnoreCase.Equals(
                             packages[2], PerformanceTestContract.DpaPackageId) ||
                         packages.Contains(PerformanceTestContract.CircinusPackageId,
                             StringComparer.OrdinalIgnoreCase) ||
                         string.IsNullOrWhiteSpace(test.DiagnosticSelector))) ||
                (!dpa && (!StringComparer.OrdinalIgnoreCase.Equals(
                              packages[2], PerformanceTestContract.CircinusPackageId) ||
                          !string.IsNullOrWhiteSpace(test.DiagnosticSelector))))
                throw Failure("manifest_tests_invalid", "A staged performance profiler declaration is invalid.");
        }
    }

    private static bool MatchesActiveGroup(
        IReadOnlyList<string> declaredPackageIds,
        IReadOnlyList<string> activePackageIds)
    {
        if (declaredPackageIds.Count + 1 != activePackageIds.Count)
        {
            return false;
        }

        for (var index = 0; index < declaredPackageIds.Count; index++)
        {
            if (!StringComparer.OrdinalIgnoreCase.Equals(
                    Normalize(declaredPackageIds[index]),
                    Normalize(activePackageIds[index])))
            {
                return false;
            }
        }

        return StringComparer.OrdinalIgnoreCase.Equals(
            Normalize(activePackageIds[activePackageIds.Count - 1]),
            EndToEndTestContract.GatewayPackageId);
    }

    private static void ValidateDependencies(
        Assembly assembly,
        IReadOnlyList<EndToEndBundleDependency> manifestDependencies)
    {
        if (manifestDependencies is null)
        {
            throw Failure("dependency_identity_mismatch", "The E2E dependency identity list is missing.");
        }

        var manifestIdentities = manifestDependencies
            .Select(ValidateDependencyDocument)
            .OrderBy(identity => identity, StringComparer.Ordinal)
            .ToArray();
        var references = assembly.GetReferencedAssemblies();
        var compiledIdentities = references
            .Select(GatewayEndToEndAssemblyIdentity.FormatReference)
            .OrderBy(identity => identity, StringComparer.Ordinal)
            .ToArray();
        if (!manifestIdentities.SequenceEqual(compiledIdentities, StringComparer.Ordinal))
        {
            throw Failure(
                "dependency_identity_mismatch",
                "The compiled E2E dependency identities differ from discovery.");
        }

        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var reference in references)
        {
            var matchingLoaded = loadedAssemblies.Any(loaded =>
            {
                try
                {
                    return GatewayIntegrationTestAssemblyIdentity.Matches(reference, loaded.GetName());
                }
                catch
                {
                    return false;
                }
            });
            if (!matchingLoaded)
            {
                throw Failure(
                    "dependency_identity_mismatch",
                    "The exact compiled E2E dependency identity '" +
                    GatewayEndToEndAssemblyIdentity.FormatReference(reference) +
                    "' is not loaded.");
            }
        }
    }

    private static string ValidateDependencyDocument(EndToEndBundleDependency dependency)
    {
        if (dependency is null ||
            string.IsNullOrWhiteSpace(dependency.Name) ||
            string.IsNullOrWhiteSpace(dependency.Version) ||
            string.IsNullOrWhiteSpace(dependency.Culture) ||
            (dependency.KeyKind != "PublicKey" && dependency.KeyKind != "PublicKeyToken") ||
            string.IsNullOrWhiteSpace(dependency.PublicKeyOrToken) ||
            string.IsNullOrWhiteSpace(dependency.Identity))
        {
            throw Failure("dependency_identity_mismatch", "An E2E dependency identity document is incomplete.");
        }

        var reconstructed = dependency.Name + ", Version=" + dependency.Version +
                            ", Culture=" + dependency.Culture + ", " +
                            dependency.KeyKind + "=" + dependency.PublicKeyOrToken;
        if (!StringComparer.Ordinal.Equals(reconstructed, dependency.Identity))
        {
            throw Failure(
                "dependency_identity_mismatch",
                "An E2E dependency identity document is internally inconsistent.");
        }

        return dependency.Identity;
    }

    private static IReadOnlyList<CompiledTest> ReadCompiledTests(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch
        {
            throw Failure(
                "compiled_test_reflection_failed",
                "The staged E2E test types could not be reflected; arbitrary exception text was suppressed.");
        }

        var tests = new List<CompiledTest>();
        foreach (var type in BoundedCompiledTestTypes(types))
        {
            var customAttributes = CustomAttributeData.GetCustomAttributes(type);
            var attributes = customAttributes
                .Where(attribute => attribute.AttributeType == typeof(RimWorldEndToEndTestAttribute))
                .ToArray();
            var performanceAttributes = customAttributes
                .Where(attribute => attribute.AttributeType == typeof(RimWorldPerformanceTestAttribute))
                .ToArray();
            if (attributes.Length == 0 && performanceAttributes.Length == 0)
            {
                continue;
            }

            if (attributes.Length > 0 && performanceAttributes.Length > 0)
                throw Failure("compiled_test_manifest_mismatch", "A compiled type declares both E2E and performance contracts.");

            if (attributes.Length > 0)
            {
                if (attributes.Length != 1)
                    throw Failure("compiled_test_manifest_mismatch", "A compiled E2E type has multiple test attributes.");
                tests.Add(ReadCompiledTest(type, attributes[0]));
                continue;
            }

            if (performanceAttributes.Length != 1)
                throw Failure("compiled_test_manifest_mismatch", "A compiled performance type has multiple attributes.");
            try
            {
                tests.Add(ReadCompiledPerformanceTest(PerformanceTestContract.Describe(type)));
            }
            catch
            {
                throw Failure("compiled_test_manifest_mismatch", "A compiled performance type violates its runtime contract.");
            }
        }

        return tests.OrderBy(test => test.Id, StringComparer.Ordinal).ToArray();
    }

    internal static IReadOnlyList<Type> BoundedCompiledTestTypes(IEnumerable<Type> types)
    {
        if (types is null) throw new ArgumentNullException(nameof(types));
        var attributed = new List<Type>();
        foreach (var type in types)
        {
            if (type is null) continue;
            var metadata = CustomAttributeData.GetCustomAttributes(type);
            if (!metadata.Any(attribute =>
                    attribute.AttributeType == typeof(RimWorldEndToEndTestAttribute) ||
                    attribute.AttributeType == typeof(RimWorldPerformanceTestAttribute)))
            {
                continue;
            }

            if (attributed.Count == PerformanceTestContract.MaximumBenchmarks)
            {
                throw Failure(
                    "compiled_test_manifest_mismatch",
                    $"The compiled test assembly exceeds the published {PerformanceTestContract.MaximumBenchmarks}-test ceiling.");
            }

            attributed.Add(type);
        }

        return attributed.OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray();
    }

    private static CompiledTest ReadCompiledTest(Type type, CustomAttributeData attribute)
    {
        if (!type.IsClass || type.IsAbstract ||
            !typeof(IRimWorldEndToEndTest).IsAssignableFrom(type) ||
            type.GetConstructor(Type.EmptyTypes) is null ||
            attribute.ConstructorArguments.Count != 3)
        {
            throw Failure("compiled_test_manifest_mismatch", "A compiled E2E type violates the runtime contract.");
        }

        var id = attribute.ConstructorArguments[0].Value as string ?? string.Empty;
        var owner = Normalize(attribute.ConstructorArguments[1].Value as string);
        var packages = ReadAttributeStringArray(attribute.ConstructorArguments[2]);
        var maxFrames = 3_600;
        var maxGameTicks = 60_000;
        var maxWallClockSeconds = 120;
        foreach (var named in attribute.NamedArguments)
        {
            var value = named.TypedValue.Value is int number ? number : 0;
            switch (named.MemberName)
            {
                case "MaxFrames":
                    maxFrames = value;
                    break;
                case "MaxGameTicks":
                    maxGameTicks = value;
                    break;
                case "MaxWallClockSeconds":
                    maxWallClockSeconds = value;
                    break;
            }
        }

        return new CompiledTest(
            id,
            owner,
            packages,
            type.FullName ?? type.Name,
            maxFrames,
            maxGameTicks,
            maxWallClockSeconds,
            type,
            EndToEndBundleTest.EndToEndKind,
            performanceDescriptor: null);
    }

    private static CompiledTest ReadCompiledPerformanceTest(PerformanceTestDescriptor descriptor)
    {
        var deadline = PerformanceDeadline(descriptor.WarmUpTicks, descriptor.SampleTicks);
        return new CompiledTest(
            descriptor.Id,
            descriptor.StagingOwnerPackageId,
            descriptor.ActivePackageIds,
            descriptor.TestType.FullName ?? descriptor.TestType.Name,
            deadline.MaxFrames,
            deadline.MaxGameTicks,
            deadline.MaxWallClockSeconds,
            descriptor.TestType,
            EndToEndBundleTest.PerformanceKind,
            descriptor);
    }

    private static (int MaxFrames, int MaxGameTicks, int MaxWallClockSeconds) PerformanceDeadline(
        int warmUpTicks,
        int sampleTicks)
    {
        var deadline = PerformanceBundleDeadline.Calculate(warmUpTicks, sampleTicks);
        return (deadline.MaxFrames, deadline.MaxGameTicks, deadline.MaxWallClockSeconds);
    }

    private static IReadOnlyList<string> ReadAttributeStringArray(CustomAttributeTypedArgument argument)
    {
        if (argument.Value is not IList<CustomAttributeTypedArgument> values)
        {
            return Array.Empty<string>();
        }

        return values.Select(value => Normalize(value.Value as string)).ToArray();
    }

    private static void ValidateCompiledManifest(
        IReadOnlyList<CompiledTest> compiled,
        IReadOnlyList<EndToEndBundleTest> manifest)
    {
        var orderedManifest = manifest.OrderBy(test => test.Id, StringComparer.Ordinal).ToArray();
        var manifestIsPerformanceOnly = orderedManifest.All(test =>
            StringComparer.Ordinal.Equals(
                string.IsNullOrWhiteSpace(test.Kind) ? EndToEndBundleTest.EndToEndKind : test.Kind,
                EndToEndBundleTest.PerformanceKind));
        var compiledIsPerformanceOnly = compiled.All(test => test.PerformanceDescriptor is not null);
        ValidateCompiledTestIds(
            compiled.Select(test => test.Id),
            compiled.Where(test => test.PerformanceDescriptor is not null).Select(test => test.Id));

        if (!manifestIsPerformanceOnly || !compiledIsPerformanceOnly)
        {
            if (compiled.Count != orderedManifest.Length)
            {
                throw Failure(
                    "compiled_test_manifest_mismatch",
                    "An ordinary or mixed E2E manifest must list every compiled test exactly once.");
            }
        }

        var compiledById = compiled.ToDictionary(test => test.Id, StringComparer.Ordinal);
        var effectivePerformanceDescriptors = new List<PerformanceTestDescriptor>();
        foreach (var expected in orderedManifest)
        {
            if (!compiledById.TryGetValue(expected.Id, out var actual))
            {
                throw Failure("compiled_test_manifest_mismatch", "A staged E2E test is absent from the compiled assembly.");
            }
            var effectiveDescriptor = EffectivePerformanceDescriptor(actual.PerformanceDescriptor, expected);
            var effectivePackages = effectiveDescriptor?.ActivePackageIds ?? actual.ActivePackageIds;
            if (!StringComparer.Ordinal.Equals(actual.Id, expected.Id) ||
                !StringComparer.Ordinal.Equals(actual.TypeName, expected.TypeName) ||
                !StringComparer.Ordinal.Equals(actual.Kind,
                    string.IsNullOrWhiteSpace(expected.Kind)
                        ? EndToEndBundleTest.EndToEndKind
                        : expected.Kind) ||
                !StringComparer.OrdinalIgnoreCase.Equals(actual.OwnerPackageId, expected.OwnerPackageId) ||
                !effectivePackages.SequenceEqual(expected.ActivePackageIds, StringComparer.OrdinalIgnoreCase) ||
                actual.MaxFrames != expected.MaxFrames ||
                actual.MaxGameTicks != expected.MaxGameTicks ||
                actual.MaxWallClockSeconds != expected.MaxWallClockSeconds)
            {
                throw Failure("compiled_test_manifest_mismatch", "The compiled E2E test metadata differs from discovery.");
            }

            if (effectiveDescriptor is not null &&
                !PerformanceManifestMatches(effectiveDescriptor, expected))
            {
                throw Failure(
                    "compiled_test_manifest_mismatch",
                    "The compiled performance metadata differs from discovery.");
            }


            if (manifestIsPerformanceOnly)
            {
                if (effectiveDescriptor is null)
                {
                    throw Failure(
                        "compiled_test_manifest_mismatch",
                        "A staged performance manifest resolves a non-performance compiled test.");
                }

                effectivePerformanceDescriptors.Add(effectiveDescriptor);
            }
        }

        if (manifestIsPerformanceOnly)
        {
            try
            {
                PerformanceTestContract.ValidateAndGroupDescriptors(effectivePerformanceDescriptors);
            }
            catch (PerformanceContractException)
            {
                throw Failure(
                    "compiled_test_manifest_mismatch",
                    "A filtered performance manifest does not contain a complete compatible comparison family and required controls.");
            }
        }
    }

    internal static void ValidateCompiledTestIds(
        IEnumerable<string> compiledIds,
        IEnumerable<string> compiledPerformanceIds)
    {
        var duplicateCompiled = compiledIds
            .GroupBy(id => id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateCompiled is not null)
        {
            throw Failure("compiled_test_manifest_mismatch", "The compiled E2E test list contains a duplicate ID.");
        }

        var duplicateCompiledPerformance = compiledPerformanceIds
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateCompiledPerformance is not null)
        {
            throw Failure(
                "compiled_test_manifest_mismatch",
                "The compiled performance test list contains a case-insensitive duplicate ID.");
        }
    }

    private static PerformanceTestDescriptor? EffectivePerformanceDescriptor(
        PerformanceTestDescriptor? canonical,
        EndToEndBundleTest manifest)
    {
        if (canonical is null) return null;
        var profiler = string.IsNullOrWhiteSpace(manifest.PerformanceProfiler)
            ? "circinus"
            : manifest.PerformanceProfiler!.Trim().ToLowerInvariant();
        return profiler == "dpa"
            ? PerformanceTestContract.ForDpaDiagnostic(
                canonical,
                manifest.DiagnosticSelector ?? string.Empty)
            : canonical;
    }

    private static bool PerformanceManifestMatches(
        PerformanceTestDescriptor actual,
        EndToEndBundleTest expected)
    {
        var selectors = expected.MethodSelectors ?? Array.Empty<PerformanceBundleMethodSelector>();
        var checkpoints = expected.ThroughputCheckpoints ?? Array.Empty<PerformanceBundleThroughputCheckpoint>();
        return StringComparer.OrdinalIgnoreCase.Equals(
                   actual.MeasuredSubjectPackageId,
                   expected.MeasuredSubjectPackageId) &&
               StringComparer.Ordinal.Equals(actual.WorkloadVersion, expected.WorkloadVersion) &&
               StringComparer.Ordinal.Equals(actual.ComparisonId, expected.ComparisonId) &&
               actual.WarmUpTicks == expected.WarmUpTicks &&
               actual.SampleTicks == expected.SampleTicks &&
               (int)actual.GameSpeed == expected.GameSpeed &&
               actual.Repetitions == expected.Repetitions &&
               (int)actual.EvidenceLens == expected.EvidenceLens &&
               StringComparer.OrdinalIgnoreCase.Equals(
                   actual.ProductAbsentControlId,
                   expected.ProductAbsentControlId) &&
               actual.MethodSelectors.Select(selector =>
                       ((int)selector.Kind, selector.Value, selector.Category))
                   .SequenceEqual(selectors.Select(selector =>
                       (selector.Kind, selector.Value, selector.Category))) &&
               actual.ThroughputCheckpoints.Select(checkpoint =>
                       (checkpoint.Id, checkpoint.MinimumCount))
                    .SequenceEqual(checkpoints.Select(checkpoint =>
                        (checkpoint.Id, checkpoint.MinimumCount))) &&
               ((actual.Profiler == PerformanceProfilerKind.DpaDiagnostic &&
                 StringComparer.OrdinalIgnoreCase.Equals(expected.PerformanceProfiler, "dpa")) ||
                (actual.Profiler == PerformanceProfilerKind.Circinus &&
                 (string.IsNullOrWhiteSpace(expected.PerformanceProfiler) ||
                  StringComparer.OrdinalIgnoreCase.Equals(expected.PerformanceProfiler, "circinus"))));
    }

    private static void EnsureRegularFile(string path, string code, string message)
    {
        var info = new FileInfo(path);
        if (!info.Exists || (info.Attributes & FileAttributes.ReparsePoint) != 0 || info.Length <= 0)
        {
            throw Failure(code, message);
        }
    }

    private static string ComputeSha256(byte[] bytes)
    {
        using var sha256 = SHA256.Create();
        return GatewayEndToEndAssemblyIdentity.FormatKey(sha256.ComputeHash(bytes));
    }

    private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static GatewayEndToEndCatalogException Failure(string code, string message) => new(code, message);

    private sealed class CompiledTest
    {
        public CompiledTest(
            string id,
            string ownerPackageId,
            IReadOnlyList<string> activePackageIds,
            string typeName,
            int maxFrames,
            int maxGameTicks,
            int maxWallClockSeconds,
            Type testType,
            string kind,
            PerformanceTestDescriptor? performanceDescriptor)
        {
            Id = id;
            OwnerPackageId = ownerPackageId;
            ActivePackageIds = activePackageIds;
            TypeName = typeName;
            MaxFrames = maxFrames;
            MaxGameTicks = maxGameTicks;
            MaxWallClockSeconds = maxWallClockSeconds;
            TestType = testType;
            Kind = kind;
            PerformanceDescriptor = performanceDescriptor;
        }

        public string Id { get; }
        public string OwnerPackageId { get; }
        public IReadOnlyList<string> ActivePackageIds { get; }
        public string TypeName { get; }
        public int MaxFrames { get; }
        public int MaxGameTicks { get; }
        public int MaxWallClockSeconds { get; }
        public Type TestType { get; }
        public string Kind { get; }
        public PerformanceTestDescriptor? PerformanceDescriptor { get; }
    }

    private sealed class GatewayEndToEndCatalogException : Exception
    {
        public GatewayEndToEndCatalogException(string code, string message) : base(message)
        {
            Code = code;
        }

        public string Code { get; }
    }
}
